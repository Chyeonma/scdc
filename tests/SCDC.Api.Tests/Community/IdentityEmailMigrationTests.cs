using Npgsql;
using SCDC.DbMigrator;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityMigrationTests
{
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
        await using var check = new NpgsqlCommand("SELECT to_regclass('identity.email_deliveries') IS NULL AND NOT EXISTS(SELECT 1 FROM identity.schema_migrations WHERE version=2026112601)", db);
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
