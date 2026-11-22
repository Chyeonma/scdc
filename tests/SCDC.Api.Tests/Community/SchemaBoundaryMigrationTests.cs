using System.Diagnostics;
using Npgsql;
using SCDC.DbMigrator;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityMigrationTests
{
    private async Task BoundaryBaselineAsync(bool seed = true)
    {
        var bootstrap = await ReadAsync("database/postgres/schema.sql");
        await ExecuteAsync(bootstrap[..bootstrap.IndexOf("-- S2-04 bootstrap snapshot;", StringComparison.Ordinal)] + "COMMIT;");
        if (seed) await ExecuteAsync(await ReadAsync("database/postgres/seed.sql"));
    }

    private Task<string> BoundarySqlAsync(string file) => ReadAsync("database/postgres/migrations/" + file);
    private async Task<bool> ApplyBoundaryAsync(string file, string? suffix = null) =>
        await SchemaBoundaryMigration.ApplyAsync(_connectionString, file, await BoundarySqlAsync(file) + suffix);

    private async Task<long> BoundaryScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private Task<long> CrossFksAsync(string schema) => BoundaryScalarAsync($"""
        SELECT count(*) FROM pg_constraint c JOIN pg_class t ON t.oid=c.conrelid
        JOIN pg_namespace n ON n.oid=t.relnamespace JOIN pg_class r ON r.oid=c.confrelid
        JOIN pg_namespace rn ON rn.oid=r.relnamespace
        WHERE c.contype='f' AND n.nspname='{schema}' AND rn.nspname<>n.nspname
        """);

    private async Task<Dictionary<string, string>> BusinessSnapshotAsync()
    {
        var tables = new List<string>();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using (var query = new NpgsqlCommand("""
            SELECT table_schema||'.'||table_name FROM information_schema.tables
            WHERE table_schema IN ('identity','community','messaging') AND table_type='BASE TABLE'
              AND table_name<>'schema_migrations' ORDER BY table_schema,table_name
            """, connection))
        await using (var reader = await query.ExecuteReaderAsync())
            while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        var result = new Dictionary<string, string>();
        foreach (var table in tables) result[table] = await SnapshotAsync(table);
        return result;
    }

    private async Task<string> ConstraintsAsync(bool localOnly = false)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"""
            SELECT coalesce(jsonb_agg(jsonb_build_array(n.nspname,t.relname,c.conname,pg_get_constraintdef(c.oid))
              ORDER BY n.nspname,t.relname,c.conname),'[]')::text
            FROM pg_constraint c JOIN pg_class t ON t.oid=c.conrelid JOIN pg_namespace n ON n.oid=t.relnamespace
            LEFT JOIN pg_class r ON r.oid=c.confrelid
            WHERE n.nspname IN ('identity','community','messaging','moderation')
            {(localOnly ? "AND (c.contype<>'f' OR t.relnamespace=r.relnamespace) AND n.nspname<>'moderation'" : "")}
            """, connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SchemaBoundary_migrations_in_either_order_preserve_all_business_rows_and_local_invariants(bool messagingFirst)
    {
        await BoundaryBaselineAsync();
        await ExecuteAsync("""
            INSERT INTO community.operations(actor_user_id,kind,scope_id,client_operation_id,fingerprint_version,key_id,fingerprint,resource_id)
                SELECT owner_user_id,'create_server','00000000-0000-0000-0000-000000000000',gen_random_uuid(),1,'retained',decode(repeat('ab',32),'hex'),id FROM community.servers LIMIT 1;
            INSERT INTO messaging.send_operations(space_id,author_user_id,client_message_id,message_id,fingerprint_version,key_id,fingerprint)
                SELECT space_id,author_user_id,gen_random_uuid(),id,1,'retained',decode(repeat('cd',32),'hex') FROM messaging.messages LIMIT 1;
            UPDATE messaging.outbox_events SET attempt_count=3,last_error='retained',lease_token=gen_random_uuid(),lease_expires_at='2026-12-01Z'
                WHERE published_at IS NULL;
            """);
        var before = await BusinessSnapshotAsync();
        var local = await ConstraintsAsync(true);
        var triggers = await SnapshotAsync("(SELECT n.nspname,t.relname,g.tgname,pg_get_triggerdef(g.oid) AS definition FROM pg_trigger g JOIN pg_class t ON t.oid=g.tgrelid JOIN pg_namespace n ON n.oid=t.relnamespace WHERE NOT g.tgisinternal AND n.nspname IN ('identity','community','messaging'))");
        Assert.Equal(9, await CrossFksAsync("community"));
        Assert.Equal(16, await CrossFksAsync("messaging"));
        Assert.Equal(7, await CrossFksAsync("moderation"));
        var files = messagingFirst
            ? new[] { SchemaBoundaryMigration.MessagingFile, SchemaBoundaryMigration.CommunityFile }
            : new[] { SchemaBoundaryMigration.CommunityFile, SchemaBoundaryMigration.MessagingFile };
        foreach (var file in files)
        {
            Assert.True(await ApplyBoundaryAsync(file));
            Assert.False(await ApplyBoundaryAsync(file));
            await Assert.ThrowsAsync<MigrationPreflightException>(() => ApplyBoundaryAsync(file, "\n-- changed"));
        }
        var after = await BusinessSnapshotAsync();
        Assert.Equal(before.Keys.Order(), after.Keys.Order());
        foreach (var (table, rows) in before) Assert.Equal(rows, after[table]);
        Assert.Equal(local, await ConstraintsAsync(true));
        Assert.Equal(triggers, await SnapshotAsync("(SELECT n.nspname,t.relname,g.tgname,pg_get_triggerdef(g.oid) AS definition FROM pg_trigger g JOIN pg_class t ON t.oid=g.tgrelid JOIN pg_namespace n ON n.oid=t.relnamespace WHERE NOT g.tgisinternal AND n.nspname IN ('identity','community','messaging'))"));
        Assert.Equal(0, await CrossFksAsync("community"));
        Assert.Equal(0, await CrossFksAsync("messaging"));
        Assert.Equal(0, await BoundaryScalarAsync("SELECT count(*) FROM pg_namespace WHERE nspname='moderation'"));
        Assert.Equal(0, await BoundaryScalarAsync("SELECT count(*) FROM pg_views WHERE schemaname='messaging' AND viewname IN ('v_space_overview','v_message_timeline')"));
    }

    [Theory]
    [InlineData("actions")]
    [InlineData("message_reports")]
    public async Task SchemaBoundary_populated_moderation_aborts_without_losing_rows_constraints_views_or_ledger(string table)
    {
        await BoundaryBaselineAsync();
        await ExecuteAsync(table == "actions"
            ? "INSERT INTO moderation.actions(moderator_user_id,target_user_id,action_type,reason,metadata) SELECT id,id,'retained','retained-reason','{\"kept\":true}' FROM identity.users LIMIT 1"
            : "INSERT INTO moderation.message_reports(message_id,reporter_user_id,reason_code,details,message_snapshot) SELECT id,author_user_id,'retained','retained-details','{\"kept\":true}' FROM messaging.messages LIMIT 1");
        var rows = await SnapshotAsync("moderation." + table);
        var constraints = await ConstraintsAsync();
        var ledger = await SnapshotAsync("messaging.schema_migrations");
        var error = await Assert.ThrowsAsync<MigrationPreflightException>(() => ApplyBoundaryAsync(SchemaBoundaryMigration.MessagingFile));
        Assert.Contains("Moderation contains data", error.Message);
        Assert.Equal(rows, await SnapshotAsync("moderation." + table));
        Assert.Equal(constraints, await ConstraintsAsync());
        Assert.Equal(ledger, await SnapshotAsync("messaging.schema_migrations"));
        Assert.Equal(2, await BoundaryScalarAsync("SELECT count(*) FROM pg_views WHERE schemaname='messaging'"));
        // Community can still migrate independently; no Moderation data is touched.
        Assert.True(await ApplyBoundaryAsync(SchemaBoundaryMigration.CommunityFile));
        Assert.Equal(rows, await SnapshotAsync("moderation." + table));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SchemaBoundary_unreviewed_constraint_drift_rolls_back(bool messaging, bool extra)
    {
        await BoundaryBaselineAsync();
        var owner = messaging ? "messaging" : "community";
        var target = messaging ? "spaces" : "servers";
        var column = messaging ? "created_by_user_id" : "owner_user_id";
        var constraint = messaging ? "fk_chat_spaces_creator" : "fk_servers_owner";
        await ExecuteAsync(extra
            ? $"ALTER TABLE {owner}.{target} ADD CONSTRAINT unreviewed_fk FOREIGN KEY({column}) REFERENCES identity.users(id)"
            : $"ALTER TABLE {owner}.{target} DROP CONSTRAINT {constraint}");
        var before = await ConstraintsAsync();
        var ledger = await SnapshotAsync(owner + ".schema_migrations");
        await Assert.ThrowsAsync<MigrationPreflightException>(() => ApplyBoundaryAsync(messaging ? SchemaBoundaryMigration.MessagingFile : SchemaBoundaryMigration.CommunityFile));
        Assert.Equal(before, await ConstraintsAsync());
        Assert.Equal(ledger, await SnapshotAsync(owner + ".schema_migrations"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SchemaBoundary_unknown_view_dependency_or_moderation_object_rolls_back(bool extraTable)
    {
        await BoundaryBaselineAsync();
        await ExecuteAsync(extraTable
            ? "CREATE TABLE moderation.unreviewed(note text); INSERT INTO moderation.unreviewed VALUES('keep')"
            : "CREATE VIEW messaging.unreviewed AS SELECT * FROM messaging.v_space_overview");
        var before = await ConstraintsAsync();
        await Assert.ThrowsAsync<PostgresException>(() => ApplyBoundaryAsync(SchemaBoundaryMigration.MessagingFile));
        Assert.Equal(before, await ConstraintsAsync());
        Assert.Equal(16, await CrossFksAsync("messaging"));
        if (extraTable) Assert.Equal(1, await BoundaryScalarAsync("SELECT count(*) FROM moderation.unreviewed"));
    }

    [Fact]
    public async Task SchemaBoundary_checks_moderation_after_waiting_for_concurrent_writer()
    {
        await BoundaryBaselineAsync();
        await using var writer = new NpgsqlConnection(_connectionString);
        await writer.OpenAsync();
        await using var transaction = await writer.BeginTransactionAsync();
        await using (var insert = new NpgsqlCommand("INSERT INTO moderation.actions(moderator_user_id,target_user_id,action_type) SELECT id,id,'concurrent' FROM identity.users LIMIT 1", writer, transaction))
            await insert.ExecuteNonQueryAsync();
        var migration = ApplyBoundaryAsync(SchemaBoundaryMigration.MessagingFile);
        var timer = Stopwatch.StartNew();
        while (await BoundaryScalarAsync($"SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND {writer.ProcessID}=ANY(pg_blocking_pids(pid))") == 0)
        {
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(3), "Migration did not reach the writer's database lock.");
            await Task.Delay(20);
        }
        Assert.False(migration.IsCompleted);
        await transaction.CommitAsync();
        await Assert.ThrowsAsync<MigrationPreflightException>(() => migration);
        Assert.Equal(1, await BoundaryScalarAsync("SELECT count(*) FROM moderation.actions"));
        Assert.Equal(16, await CrossFksAsync("messaging"));
    }

    [Fact]
    public async Task SchemaBoundary_bootstrap_and_ledger_match_both_migrations()
    {
        var bootstrap = await ReadAsync("database/postgres/schema.sql");
        Assert.Contains(await BoundarySqlAsync(SchemaBoundaryMigration.CommunityFile), bootstrap);
        Assert.Contains(await BoundarySqlAsync(SchemaBoundaryMigration.MessagingFile), bootstrap);
        await ExecuteAsync(bootstrap);
        await ExecuteAsync(await ReadAsync("database/postgres/seed.sql"));
        Assert.False(await ApplyBoundaryAsync(SchemaBoundaryMigration.CommunityFile));
        Assert.False(await ApplyBoundaryAsync(SchemaBoundaryMigration.MessagingFile));
        Assert.Equal(0, await CrossFksAsync("community"));
        Assert.Equal(0, await CrossFksAsync("messaging"));
    }

    [Fact]
    public async Task SchemaBoundary_requires_S2_03_before_changing_schema()
    {
        await ModuleLegacyAsync();
        await Assert.ThrowsAsync<MigrationPreflightException>(() => ApplyBoundaryAsync(SchemaBoundaryMigration.CommunityFile));
        await Assert.ThrowsAsync<MigrationPreflightException>(() => ApplyBoundaryAsync(SchemaBoundaryMigration.MessagingFile));
        Assert.Equal(9, await CrossFksAsync("community"));
        Assert.Equal(16, await CrossFksAsync("messaging"));
    }
}
