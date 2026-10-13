using Npgsql;
using SCDC.DbMigrator;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityMigrationTests
{
    private async Task<IReadOnlyList<Guid>> SearchBaselineAsync(int count = 1)
    {
        await ExecuteAsync(await ReadAsync("tests/SCDC.Api.Tests/Community/Fixtures/legacy-schema.sql"));
        var owner = Guid.CreateVersion7();
        await ExecuteAsync($"INSERT INTO identity.users(id,username,status) VALUES('{owner}','search_owner',1)");
        var ids = new List<Guid>();
        for (var index = 0; index < count; index++)
        {
            var id = Guid.CreateVersion7();
            ids.Add(id);
            await ExecuteAsync($"""
                INSERT INTO community.servers(id,owner_user_id,name,slug,updated_at) VALUES('{id}','{owner}','CAFÉ 👩‍💻','{id:N}','2025-01-01T00:00:00Z');
                INSERT INTO community.server_members(server_id,user_id) VALUES('{id}','{owner}');
                INSERT INTO community.roles(server_id,name,is_default,is_system) VALUES('{id}','@everyone',true,true);
                """);
        }
        await CommunityMigration.ApplyAsync(_connectionString, await ReadAsync("database/postgres/migrations/001-community-create-view.sql"),
            ids.ToDictionary(id => id, _ => new LegacyServerSettings("private", "approval")));
        return ids;
    }
    private async Task<object?> SearchScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }

    [Fact]
    public async Task Search_upgrade_backfills_multiple_batches_and_preserves_display_versions_visibility_and_memberships()
    {
        await SearchBaselineAsync(501);
        await ExecuteAsync("CREATE TABLE community.search_epoch_fixture AS SELECT server_id,user_id,membership_id FROM community.server_members");
        var sql = await ReadAsync("database/postgres/migrations/002-community-search.sql");
        Assert.True(await CommunitySearchMigration.ApplyAsync(_connectionString, sql));
        Assert.Equal(501L, await SearchScalarAsync("""
            SELECT count(*) FROM community.servers s JOIN community.server_members m ON m.server_id=s.id AND m.user_id=s.owner_user_id
            JOIN community.search_epoch_fixture original ON original.server_id=m.server_id AND original.user_id=m.user_id AND original.membership_id=m.membership_id
            WHERE s.search_name='café 👩‍💻' AND s.name='CAFÉ 👩‍💻' AND s.version=1 AND s.access_version=1
              AND s.visibility=2 AND s.join_mode=2 AND s.updated_at='2025-01-01T00:00:00Z' AND m.status=1 AND m.version=1
            """));
        Assert.False(await CommunitySearchMigration.ApplyAsync(_connectionString, sql));
        await Assert.ThrowsAsync<MigrationPreflightException>(() => CommunitySearchMigration.ApplyAsync(_connectionString, sql + "\n-- changed"));
        Assert.Equal(2L, await SearchScalarAsync("SELECT count(*) FROM common.schema_migrations WHERE module='community'"));
    }

    [Fact]
    public async Task Search_upgrade_requires_baseline_and_invalid_names_roll_back_without_repair()
    {
        await ExecuteAsync(await ReadAsync("tests/SCDC.Api.Tests/Community/Fixtures/legacy-schema.sql"));
        var sql = await ReadAsync("database/postgres/migrations/002-community-search.sql");
        await Assert.ThrowsAsync<MigrationPreflightException>(() => CommunitySearchMigration.ApplyAsync(_connectionString, sql));
        // The separate fixture below starts from the same legacy schema.
        await SearchBaselineAsync();
        await ExecuteAsync("UPDATE community.servers SET name='  '");
        await Assert.ThrowsAsync<MigrationPreflightException>(() => CommunitySearchMigration.ApplyAsync(_connectionString, sql));
        Assert.Equal("  ", await SearchScalarAsync("SELECT name FROM community.servers"));
        Assert.Equal(2, await SearchScalarAsync("SELECT version FROM community.servers"));
        Assert.Equal(0L, await SearchScalarAsync("SELECT count(*) FROM information_schema.columns WHERE table_schema='community' AND table_name='servers' AND column_name='search_name'"));
        Assert.Equal(0L, await SearchScalarAsync("SELECT count(*) FROM common.schema_migrations WHERE module='community' AND version=2"));
    }

    [Fact]
    public async Task Search_upgrade_fault_rolls_back_ddl_backfill_and_trigger_state_then_valid_upgrade_succeeds()
    {
        await SearchBaselineAsync();
        var sql = await ReadAsync("database/postgres/migrations/002-community-search.sql");
        await Assert.ThrowsAsync<PostgresException>(() => CommunitySearchMigration.ApplyAsync(_connectionString, sql + "\nSELECT 1/0;"));
        Assert.Equal(0L, await SearchScalarAsync("SELECT count(*) FROM information_schema.columns WHERE table_schema='community' AND table_name='servers' AND column_name='search_name'"));
        Assert.Equal("O", await SearchScalarAsync("SELECT tgenabled::text FROM pg_trigger WHERE tgrelid='community.servers'::regclass AND tgname='tr_servers_touch'"));
        Assert.Equal(1, await SearchScalarAsync("SELECT version FROM community.servers"));
        Assert.True(await CommunitySearchMigration.ApplyAsync(_connectionString, sql));
    }

    [Fact]
    public async Task Search_bootstrap_seed_and_upgrade_ledger_agree()
    {
        await ExecuteAsync(await ReadAsync("database/postgres/schema.sql"));
        await ExecuteAsync(await ReadAsync("database/postgres/seed.sql"));
        Assert.False(await CommunitySearchMigration.ApplyAsync(_connectionString, await ReadAsync("database/postgres/migrations/002-community-search.sql")));
        Assert.Equal(true, await SearchScalarAsync("SELECT search_name='scdc community' FROM community.servers"));
        Assert.Equal(true, await SearchScalarAsync("SELECT EXISTS(SELECT 1 FROM pg_indexes WHERE schemaname='community' AND indexname='ix_servers_public_search')"));
    }
}
