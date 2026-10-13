using Npgsql;
using SCDC.DbMigrator;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityMigrationTests
{
    private async Task<Guid> RoleBaselineAsync(int count = 1)
    {
        var id=(await SearchBaselineAsync(count))[0];
        await CommunitySearchMigration.ApplyAsync(_connectionString,await ReadAsync("database/postgres/migrations/002-community-search.sql"));
        return id;
    }
    [Fact]
    public async Task Role_upgrade_preserves_names_permissions_timestamps_and_epochs_then_replays()
    {
        var server=await RoleBaselineAsync(501);
        await ExecuteAsync($"""
            INSERT INTO community.roles(id,server_id,name,updated_at) VALUES('019a0000-0000-7000-8000-000000000111','{server}','CAFÉ','2025-01-01T00:00:00Z');
            INSERT INTO community.permissions(code,description) VALUES('manage_invites','fixture');
            INSERT INTO community.role_permissions(role_id,permission_code) VALUES('019a0000-0000-7000-8000-000000000111','manage_invites');
            INSERT INTO community.member_roles(server_id,user_id,role_id) SELECT server_id,user_id,'019a0000-0000-7000-8000-000000000111' FROM community.server_members WHERE server_id='{server}';
            """);
        var sql=await ReadAsync("database/postgres/migrations/003-community-roles.sql");
        Assert.True(await CommunityRoleMigration.ApplyAsync(_connectionString,sql));
        Assert.Equal(502L,await SearchScalarAsync("SELECT count(*) FROM community.roles WHERE name_key IS NOT NULL AND version=1"));
        Assert.Equal(true,await SearchScalarAsync("SELECT name='CAFÉ' AND name_key='café' AND version=1 AND updated_at='2025-01-01T00:00:00Z' FROM community.roles WHERE NOT is_system"));
        Assert.Equal(true,await SearchScalarAsync("SELECT mr.membership_id=m.membership_id FROM community.member_roles mr JOIN community.server_members m USING(server_id,user_id)"));
        Assert.Equal(1L,await SearchScalarAsync("SELECT count(*) FROM community.role_permissions WHERE permission_code='manage_invites'"));
        Assert.False(await CommunityRoleMigration.ApplyAsync(_connectionString,sql));
        await Assert.ThrowsAsync<MigrationPreflightException>(()=>CommunityRoleMigration.ApplyAsync(_connectionString,sql+"\n--changed"));
        Assert.Equal(1,await SearchScalarAsync("SELECT access_version FROM community.servers"));
    }
    [Theory]
    [InlineData("collision")]
    [InlineData("catalog")]
    [InlineData("name")]
    [InlineData("cap")]
    public async Task Role_upgrade_preflight_rejects_invalid_legacy_data_without_repair(string change)
    {
        var server=await RoleBaselineAsync();
        if(change=="collision")await ExecuteAsync($"INSERT INTO community.roles(server_id,name) VALUES('{server}','Café'),('{server}','Café')");
        if(change=="name")await ExecuteAsync($"INSERT INTO community.roles(server_id,name) VALUES('{server}',' X ')");
        if(change=="cap")await ExecuteAsync($"INSERT INTO community.roles(server_id,name) SELECT '{server}', 'Role '||n FROM generate_series(1,21)n");
        if(change=="catalog")await ExecuteAsync($"INSERT INTO community.permissions(code,description) VALUES('manage_roles','legacy'); INSERT INTO community.roles(server_id,name) VALUES('{server}','Legacy'); INSERT INTO community.role_permissions(role_id,permission_code) SELECT id,'manage_roles' FROM community.roles WHERE NOT is_system");
        var sql=await ReadAsync("database/postgres/migrations/003-community-roles.sql");
        await Assert.ThrowsAsync<MigrationPreflightException>(()=>CommunityRoleMigration.ApplyAsync(_connectionString,sql));
        Assert.Equal(0L,await SearchScalarAsync("SELECT count(*) FROM information_schema.columns WHERE table_schema='community' AND table_name='roles' AND column_name='name_key'"));
        Assert.Equal(0L,await SearchScalarAsync("SELECT count(*) FROM common.schema_migrations WHERE module='community' AND version=3"));
    }
    [Fact]
    public async Task Role_upgrade_requires_baseline_and_fault_rolls_back_ddl_backfill_and_trigger()
    {
        await ExecuteAsync(await ReadAsync("tests/SCDC.Api.Tests/Community/Fixtures/legacy-schema.sql"));
        var sql=await ReadAsync("database/postgres/migrations/003-community-roles.sql");
        await Assert.ThrowsAsync<MigrationPreflightException>(()=>CommunityRoleMigration.ApplyAsync(_connectionString,sql));
        await RoleBaselineAsync();
        await Assert.ThrowsAsync<PostgresException>(()=>CommunityRoleMigration.ApplyAsync(_connectionString,sql+"\nSELECT 1/0;"));
        Assert.Equal(0L,await SearchScalarAsync("SELECT count(*) FROM information_schema.columns WHERE table_schema='community' AND table_name='roles' AND column_name='name_key'"));
        Assert.Equal("O",await SearchScalarAsync("SELECT tgenabled::text FROM pg_trigger WHERE tgrelid='community.roles'::regclass AND tgname='tr_server_roles_touch'"));
        Assert.True(await CommunityRoleMigration.ApplyAsync(_connectionString,sql));
    }
    [Fact]
    public async Task Role_bootstrap_seed_agree_with_ledger_and_epoch_foreign_keys_reject_stale_grants()
    {
        await ExecuteAsync(await ReadAsync("database/postgres/schema.sql"));
        await ExecuteAsync(await ReadAsync("database/postgres/seed.sql"));
        Assert.False(await CommunityRoleMigration.ApplyAsync(_connectionString,await ReadAsync("database/postgres/migrations/003-community-roles.sql")));
        Assert.Equal(0L,await SearchScalarAsync("SELECT count(*) FROM community.member_roles mr JOIN community.server_members m USING(server_id,user_id) WHERE mr.membership_id<>m.membership_id"));
        var exception=await Assert.ThrowsAsync<PostgresException>(()=>ExecuteAsync("UPDATE community.server_members SET membership_id=uuidv7() WHERE user_id='01990000-0000-7000-8000-000000000002'"));
        Assert.Equal("23503",exception.SqlState);
    }
}
