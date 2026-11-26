using Npgsql;
using SCDC.DbMigrator;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityMigrationTests
{
    private async Task PrepareLegacyEmailTables()
    {
        await PrepareEmailBaseline();
        var legacy = await ReadAsync("database/postgres/identity/" + IdentityEmailMigration.LegacyFileName);
        await ExecuteAsync("BEGIN;\n" + legacy[..legacy.IndexOf("-- Preserve the latest link", StringComparison.Ordinal)] + "\nCOMMIT;");
    }

    private async Task<Guid> SeedLegacyEmail(bool live)
    {
        var user = Guid.CreateVersion7(); var token = Guid.CreateVersion7();
        await ExecuteAsync($"""
            INSERT INTO identity.users(id,username,status) VALUES('{user}','legacy_email',0);
            INSERT INTO identity.account_tokens(id,user_id,purpose,token_hash,created_at,expires_at)
            VALUES('{token}','{user}',1,'legacy',clock_timestamp()-interval '1 minute',clock_timestamp()+interval '30 minutes');
            INSERT INTO identity.account_token_policies(user_id,purpose,last_issued_at,active_token_id)
            VALUES('{user}',1,clock_timestamp()+interval '10 minutes',NULL);
            INSERT INTO identity.email_deliveries(id,user_id,account_token_id,outbox_event_id,purpose,recipient,
                protected_envelope,envelope_expires_at,status,next_attempt_at,created_at,terminal_at)
            VALUES(uuidv7(),'{user}','{token}',uuidv7(),1,'legacy@example.test',
                {(live ? "'legacy-cipher'" : "NULL")},clock_timestamp()+interval '30 minutes',{(live ? 0 : 3)},
                clock_timestamp(),clock_timestamp()-interval '1 minute',{(live ? "NULL" : "clock_timestamp()")});
            """);
        return user;
    }

    [Fact]
    public async Task Email_upgrade_adopts_reviewed_legacy_rows_without_resetting_cooldown_or_fabricating_v1_history()
    {
        await PrepareLegacyEmailTables();
        var user = await SeedLegacyEmail(live: false);
        Assert.True(await IdentityEmailMigration.ApplyAsync(_connectionString,
            await ReadAsync("database/postgres/identity/" + IdentityEmailMigration.FileName)));
        await using var db = new NpgsqlConnection(_connectionString); await db.OpenAsync();
        await using var check = new NpgsqlCommand($"""
            SELECT (SELECT count(*) FROM identity.email_deliveries)=1
                AND (SELECT count(*) FROM identity.schema_migrations WHERE version=2026112601)=0
                AND active_token_id IS NULL AND last_issued_at>clock_timestamp()+interval '9 minutes'
            FROM identity.account_token_policies WHERE user_id='{user}'
            """, db);
        Assert.True(await check.ExecuteScalarAsync() is true);
    }

    [Fact]
    public async Task Email_upgrade_rejects_unreviewed_legacy_shape_without_changing_rows()
    {
        await PrepareLegacyEmailTables(); await SeedLegacyEmail(live: false);
        await ExecuteAsync("ALTER TABLE identity.email_deliveries ADD COLUMN unreviewed integer;");
        await Assert.ThrowsAsync<PostgresException>(() => IdentityEmailMigration.ApplyAsync(_connectionString,
            File.ReadAllText(Path.Combine(RepoRoot, "database/postgres/identity", IdentityEmailMigration.FileName))));
        await using var db = new NpgsqlConnection(_connectionString); await db.OpenAsync();
        await using var check = new NpgsqlCommand("SELECT count(*)=1 AND NOT EXISTS(SELECT 1 FROM identity.schema_migrations WHERE version=2026112602) FROM identity.email_deliveries", db);
        Assert.True(await check.ExecuteScalarAsync() is true);
    }

    [Fact]
    public async Task Email_upgrade_refuses_live_unbound_legacy_envelopes_instead_of_discarding_them()
    {
        await PrepareLegacyEmailTables(); await SeedLegacyEmail(live: true);
        await Assert.ThrowsAsync<PostgresException>(() => IdentityEmailMigration.ApplyAsync(_connectionString,
            File.ReadAllText(Path.Combine(RepoRoot, "database/postgres/identity", IdentityEmailMigration.FileName))));
        await using var db = new NpgsqlConnection(_connectionString); await db.OpenAsync();
        await using var check = new NpgsqlCommand("SELECT protected_envelope='legacy-cipher' AND NOT EXISTS(SELECT 1 FROM identity.schema_migrations WHERE version=2026112602) FROM identity.email_deliveries", db);
        Assert.True(await check.ExecuteScalarAsync() is true);
    }

    [Fact]
    public async Task Email_upgrade_keeps_already_applied_v1_and_current_envelopes()
    {
        await PrepareEmailBaseline();
        Assert.True(await IdentityEmailMigration.ApplyLegacyAsync(_connectionString,
            await ReadAsync("database/postgres/identity/" + IdentityEmailMigration.LegacyFileName)));
        await SeedLegacyEmail(live: true);
        Assert.True(await IdentityEmailMigration.ApplyAsync(_connectionString,
            await ReadAsync("database/postgres/identity/" + IdentityEmailMigration.FileName)));
        await using var db = new NpgsqlConnection(_connectionString); await db.OpenAsync();
        await using var check = new NpgsqlCommand("SELECT count(*)=1 AND (SELECT count(*) FROM identity.schema_migrations WHERE version IN(2026112601,2026112602))=2 FROM identity.email_deliveries WHERE protected_envelope='legacy-cipher'", db);
        Assert.True(await check.ExecuteScalarAsync() is true);
    }

    private async Task PrepareEmailBaseline()
    {
        await ExecuteAsync(await ReadAsync("database/postgres/schema.sql"));
        var roles = await ReadAsync("database/postgres/module-roles.sql");
        await ExecuteAsync("BEGIN;\n" + roles[roles.IndexOf("REVOKE CREATE ON SCHEMA", StringComparison.Ordinal)..]);
    }

    [Fact]
    public async Task Email_upgrade_keeps_latest_legacy_link_and_cooldown_and_rejects_changed_checksum()
    {
        await PrepareEmailBaseline();
        var user = Guid.CreateVersion7(); var old = Guid.CreateVersion7(); var latest = Guid.CreateVersion7();
        await ExecuteAsync($"""
            INSERT INTO identity.users(id,username,status) VALUES('{user}','email_upgrade',0);
            INSERT INTO identity.account_tokens(id,user_id,purpose,token_hash,created_at,expires_at)
            VALUES('{old}','{user}',1,'old',clock_timestamp()-interval '2 minutes',clock_timestamp()+interval '28 minutes'),
                  ('{latest}','{user}',1,'latest',clock_timestamp()-interval '10 seconds',clock_timestamp()+interval '30 minutes');
            """);
        var sql = await ReadAsync("database/postgres/identity/" + IdentityEmailMigration.FileName);
        Assert.True(await IdentityEmailMigration.ApplyAsync(_connectionString, sql));
        Assert.False(await IdentityEmailMigration.ApplyAsync(_connectionString, sql));
        await Assert.ThrowsAsync<MigrationPreflightException>(() => IdentityEmailMigration.ApplyAsync(_connectionString, sql + "\n-- changed"));
        await using var db = new NpgsqlConnection(_connectionString); await db.OpenAsync();
        await using var check = new NpgsqlCommand($"""
            SELECT p.active_token_id='{latest}' AND p.last_issued_at=t.created_at
                AND t.consumed_at IS NULL AND old.consumed_at IS NOT NULL
            FROM identity.account_token_policies p JOIN identity.account_tokens t ON t.id=p.active_token_id
            JOIN identity.account_tokens old ON old.id='{old}' WHERE p.user_id='{user}'
            """, db);
        Assert.True(await check.ExecuteScalarAsync() is true);
    }

    [Fact]
    public async Task Email_upgrade_rejects_running_legacy_writer_without_partial_schema()
    {
        await PrepareEmailBaseline();
        var runtime = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ConnectionStrings__Identity")!)
            { Database = _database, Pooling = false }.ConnectionString;
        await using (var writer = new NpgsqlConnection(runtime))
        {
            await writer.OpenAsync();
            var sql = await ReadAsync("database/postgres/identity/" + IdentityEmailMigration.FileName);
            await Assert.ThrowsAsync<PostgresException>(() => IdentityEmailMigration.ApplyAsync(_connectionString, sql));
        }
        await using var db = new NpgsqlConnection(_connectionString); await db.OpenAsync();
        await using var check = new NpgsqlCommand("SELECT to_regclass('identity.email_deliveries') IS NULL AND NOT EXISTS(SELECT 1 FROM identity.schema_migrations WHERE version=2026112602)", db);
        Assert.True(await check.ExecuteScalarAsync() is true);
    }

    [Fact]
    public async Task Email_upgrade_works_on_fresh_extracted_Identity_and_grants_only_owner_tables()
    {
        await PrepareEmailBaseline();
        await ExecuteAsync("DROP SCHEMA identity CASCADE; DROP SCHEMA IF EXISTS common,community,messaging CASCADE;");
        await ExecuteAsync(await ReadAsync("database/postgres/identity/schema.sql"));
        var baseline = (await ReadAsync("database/postgres/identity/baseline.sql")).Replace(":'baseline_checksum'", "'" + new string('0', 64) + "'", StringComparison.Ordinal);
        await ExecuteAsync(baseline);
        await ExecuteAsync(await ReadAsync("database/postgres/identity/runtime-grants.sql"));
        Assert.True(await IdentityEmailMigration.ApplyAsync(_connectionString,
            await ReadAsync("database/postgres/identity/" + IdentityEmailMigration.FileName)));
        var runtime = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ConnectionStrings__Identity")!)
            { Database = _database, Pooling = false }.ConnectionString;
        await using var db = new NpgsqlConnection(runtime); await db.OpenAsync();
        await using var check = new NpgsqlCommand("SELECT count(*) FROM identity.email_deliveries", db);
        Assert.Equal(0L, await check.ExecuteScalarAsync());
        await using var protectedLedger = new NpgsqlCommand("DELETE FROM identity.schema_migrations", db);
        await Assert.ThrowsAsync<PostgresException>(() => protectedLedger.ExecuteNonQueryAsync());
    }
}
