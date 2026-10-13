using Npgsql;
using SCDC.DbMigrator;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityMigrationTests
{
    private async Task<(Guid Server, Dictionary<Guid, LegacyChannelSettings> Map)> ChannelBaselineAsync(int count = 1)
    {
        var server = await RoleBaselineAsync();
        await CommunityRoleMigration.ApplyAsync(_connectionString, await ReadAsync("database/postgres/migrations/003-community-roles.sql"));
        var map = new Dictionary<Guid, LegacyChannelSettings>();
        for (var index = 0; index < count; index++)
        {
            var id = Guid.CreateVersion7(); map[id] = new("text", "allow");
            await ExecuteAsync($"""
                INSERT INTO messaging.spaces(id,space_type) VALUES('{id}',3);
                INSERT INTO community.channels(space_id,server_id,name,topic,updated_at)
                    VALUES('{id}','{server}','room-{index:D4}','Old topic','2025-01-01T00:00:00Z');
                """);
        }
        return (server, map);
    }
    [Fact]
    public async Task Channel_upgrade_reviewed_mapping_crosses_batch_boundary_preserves_names_epochs_space_and_acl_then_replays()
    {
        var (server, map) = await ChannelBaselineAsync(501); var first = map.Keys.First(); var deleted = map.Keys.Last();
        map[first] = new("voice", "deny");
        await ExecuteAsync($"""
            ALTER TABLE community.channels DROP CONSTRAINT ck_server_channels_name;
            UPDATE community.channels SET name='CAFÉ 👩‍💻',visibility=2 WHERE space_id='{first}';
            UPDATE messaging.spaces SET status=3,deleted_at='2025-02-01T00:00:00Z' WHERE id='{deleted}';
            INSERT INTO community.channel_role_overrides(space_id,server_id,role_id,permission_code,effect)
                SELECT '{first}',server_id,id,'channel_view',2 FROM community.roles WHERE server_id='{server}' AND is_system;
            """);
        // The legacy fixture update touched its timestamp; capture before upgrading.
        var before = await SearchScalarAsync($"SELECT updated_at FROM community.channels WHERE space_id='{first}'");
        var sql = await ReadAsync("database/postgres/migrations/004-community-channels-access.sql");
        Assert.True(await CommunityChannelMigration.ApplyAsync(_connectionString, sql, map));
        Assert.Equal(501L, await SearchScalarAsync("SELECT count(*) FROM community.channels WHERE name_key IS NOT NULL AND version=1 AND access_version=1"));
        Assert.Equal(true, await SearchScalarAsync($"SELECT name='CAFÉ 👩‍💻' AND name_key='café 👩‍💻' AND kind=2 AND default_view=2 FROM community.channels WHERE space_id='{first}'"));
        Assert.Equal(before, await SearchScalarAsync($"SELECT updated_at FROM community.channels WHERE space_id='{first}'"));
        Assert.Equal(true, await SearchScalarAsync($"SELECT status=3 AND deleted_at='2025-02-01T00:00:00Z' FROM community.channels WHERE space_id='{deleted}'"));
        Assert.Equal(1L, await SearchScalarAsync("SELECT count(*) FROM community.channel_role_overrides"));
        Assert.False(await CommunityChannelMigration.ApplyAsync(_connectionString, sql));
        await Assert.ThrowsAsync<MigrationPreflightException>(() => CommunityChannelMigration.ApplyAsync(_connectionString, sql + "\n--changed"));
        Assert.Equal(1, await SearchScalarAsync($"SELECT access_version FROM community.servers WHERE id='{server}'"));
    }
    [Theory]
    [InlineData("map")]
    [InlineData("read-only")]
    [InlineData("archived")]
    [InlineData("collision")]
    public async Task Channel_upgrade_preflight_requires_review_and_preserves_invalid_legacy_data(string change)
    {
        var (server, map) = await ChannelBaselineAsync(); var id = map.Keys.Single();
        if (change == "map") map.Clear();
        if (change == "read-only") await ExecuteAsync($"UPDATE community.channels SET visibility=3 WHERE space_id='{id}'");
        if (change == "archived") await ExecuteAsync($"UPDATE messaging.spaces SET status=2 WHERE id='{id}'");
        if (change == "collision")
        {
            var other = Guid.CreateVersion7(); map[other] = new("text", "allow");
            await ExecuteAsync($"""
                ALTER TABLE community.channels DROP CONSTRAINT ck_server_channels_name;
                UPDATE community.channels SET name='Café' WHERE space_id='{id}';
                INSERT INTO messaging.spaces(id,space_type) VALUES('{other}',3);
                INSERT INTO community.channels(space_id,server_id,name) VALUES('{other}','{server}','Café');
                """);
        }
        await Assert.ThrowsAsync<MigrationPreflightException>(() => CommunityChannelMigration.ApplyAsync(_connectionString,
            File.ReadAllText(Path.Combine(RepoRoot, "database/postgres/migrations/004-community-channels-access.sql")), map));
        Assert.Equal(0L, await SearchScalarAsync("SELECT count(*) FROM information_schema.columns WHERE table_schema='community' AND table_name='channels' AND column_name='name_key'"));
        Assert.Equal(0L, await SearchScalarAsync("SELECT count(*) FROM common.schema_migrations WHERE module='community' AND version=4"));
    }
    [Fact]
    public async Task Channel_upgrade_fault_rolls_back_ddl_keys_and_trigger_and_requires_baselines()
    {
        var sql = await ReadAsync("database/postgres/migrations/004-community-channels-access.sql");
        await ExecuteAsync(await ReadAsync("tests/SCDC.Api.Tests/Community/Fixtures/legacy-schema.sql"));
        await Assert.ThrowsAsync<MigrationPreflightException>(() => CommunityChannelMigration.ApplyAsync(_connectionString, sql));
        var (_, map) = await ChannelBaselineAsync();
        await Assert.ThrowsAsync<PostgresException>(() => CommunityChannelMigration.ApplyAsync(_connectionString, sql + "\nSELECT 1/0;", map));
        Assert.Equal("O", await SearchScalarAsync("SELECT tgenabled::text FROM pg_trigger WHERE tgrelid='community.channels'::regclass AND tgname='tr_server_channels_touch'"));
        Assert.Equal(0L, await SearchScalarAsync("SELECT count(*) FROM common.schema_migrations WHERE module='community' AND version=4"));
        Assert.True(await CommunityChannelMigration.ApplyAsync(_connectionString, sql, map));
    }
    [Fact]
    public async Task Channel_bootstrap_seed_and_ledger_agree_with_active_name_uniqueness()
    {
        await ExecuteAsync(await ReadAsync("database/postgres/schema.sql")); await ExecuteAsync(await ReadAsync("database/postgres/seed.sql"));
        Assert.False(await CommunityChannelMigration.ApplyAsync(_connectionString, await ReadAsync("database/postgres/migrations/004-community-channels-access.sql")));
        Assert.Equal(2L, await SearchScalarAsync("SELECT count(*) FROM community.channels WHERE name_key IS NOT NULL AND kind=1 AND default_view=1"));
        Assert.Equal(true, await SearchScalarAsync("SELECT EXISTS(SELECT 1 FROM pg_indexes WHERE schemaname='community' AND indexname='ux_channel_active_name_key')"));
    }
}
