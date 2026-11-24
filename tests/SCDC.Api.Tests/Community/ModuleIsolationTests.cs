using Npgsql;
using SCDC.Api.Tests.Identity;

namespace SCDC.Api.Tests.Community;

[Collection("DM database")]
public sealed class ModuleIsolationTests(RedisAccessDatabase database) : IClassFixture<RedisAccessDatabase>
{
    [Fact]
    public Task Bootstrap_roles_have_no_cross_module_schema_object_or_column_privileges() => AssertIsolation();

    [Fact]
    public async Task Missing_prerequisite_rolls_back_before_removing_legacy_privileges()
    {
        var checksum = (string)(await database.Sql("DELETE FROM messaging.schema_migrations WHERE module='messaging' AND version=2026112409 RETURNING checksum"))!;
        Assert.Matches("^[a-f0-9]{64}$", checksum);
        var roles = await File.ReadAllTextAsync(Path.Combine(RedisAccessDatabase.Repo(), "database/postgres/module-roles.sql"));
        var grants = "BEGIN;\n" + roles[roles.IndexOf("REVOKE CREATE ON SCHEMA public FROM PUBLIC;", StringComparison.Ordinal)..];
        try
        {
            await database.Sql("GRANT USAGE ON SCHEMA identity TO scdc_community; GRANT UPDATE(id) ON identity.users TO scdc_community");
            var failure = await Assert.ThrowsAsync<PostgresException>(() => database.Sql(grants));
            Assert.Contains("S2-11 requires", failure.MessageText);
            Assert.Equal(true, await database.Sql("SELECT has_column_privilege('scdc_community','identity.users','id','UPDATE')"));
        }
        finally
        {
            await database.Sql($"INSERT INTO messaging.schema_migrations(module,version,checksum) VALUES('messaging',2026112409,'{checksum}')");
            await database.Sql(grants);
        }
    }

    [Fact]
    public async Task Reapplying_roles_removes_existing_temporary_grants_and_preserves_data()
    {
        var seed = await File.ReadAllLinesAsync(Path.Combine(RedisAccessDatabase.Repo(), "database/postgres/seed.sql"));
        await database.Sql(string.Join('\n', seed.Where(line => !line.StartsWith('\\'))));
        var before = await database.Sql("SELECT md5(jsonb_agg(to_jsonb(u) ORDER BY id)::text) FROM identity.users u");
        await database.Sql("""
            GRANT USAGE ON SCHEMA identity TO scdc_community,scdc_messaging;
            GRANT SELECT ON identity.users,identity.auth_sessions,identity.user_emails,identity.user_security_states TO scdc_community,scdc_messaging;
            GRANT SELECT ON identity.user_profiles TO scdc_messaging;
            GRANT UPDATE(id) ON identity.users TO scdc_community,scdc_messaging;
            GRANT UPDATE(id) ON identity.auth_sessions TO scdc_messaging;
            GRANT USAGE ON SCHEMA messaging TO scdc_community;
            GRANT INSERT ON messaging.spaces TO scdc_community;
            GRANT SELECT(id,space_type),UPDATE(id) ON messaging.spaces TO scdc_community;
            GRANT EXECUTE ON FUNCTION messaging.assert_direct_conversation_membership(uuid) TO scdc_community;
            """);
        Assert.Equal(true, await database.Sql("SELECT has_column_privilege('scdc_community','identity.users','id','UPDATE')"));
        var roles = await File.ReadAllTextAsync(Path.Combine(RedisAccessDatabase.Repo(), "database/postgres/module-roles.sql"));
        var grants = "BEGIN;\n" + roles[roles.IndexOf("REVOKE CREATE ON SCHEMA public FROM PUBLIC;", StringComparison.Ordinal)..];
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await database.Sql(grants);
            await AssertIsolation();
            Assert.Equal(before, await database.Sql("SELECT md5(jsonb_agg(to_jsonb(u) ORDER BY id)::text) FROM identity.users u"));
        }
    }

    private async Task AssertIsolation()
    {
        foreach (var owner in new[] { "Identity", "Community", "Messaging" })
        {
            await using var connection = new NpgsqlConnection(database.Configuration["ConnectionStrings:" + owner]);
            await connection.OpenAsync();
            async Task<object?> Query(string sql)
            {
                await using var command = new NpgsqlCommand(sql, connection);
                return await command.ExecuteScalarAsync();
            }
            Assert.Equal("scdc_" + owner.ToLowerInvariant(), await Query("SELECT current_user"));
            Assert.Equal(true, await Query($"SELECT has_schema_privilege(current_user,'{owner.ToLowerInvariant()}','USAGE')"));
            foreach (var other in new[] { "identity", "community", "messaging" }.Except([owner.ToLowerInvariant()]))
            {
                Assert.Equal(false, await Query($"SELECT has_schema_privilege(current_user,'{other}','USAGE,CREATE')"));
                Assert.Equal(false, await Query($"""
                    SELECT EXISTS(SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                    WHERE n.nspname='{other}' AND c.relkind IN ('r','p','v','m','f') AND
                    (has_table_privilege(current_user,c.oid,'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER')
                     OR has_any_column_privilege(current_user,c.oid,'SELECT,INSERT,UPDATE,REFERENCES')))
                    """));
                Assert.Equal(false, await Query($"""
                    SELECT EXISTS(SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                    WHERE n.nspname='{other}' AND c.relkind='S' AND has_sequence_privilege(current_user,c.oid,'USAGE,SELECT,UPDATE'))
                    """));
                Assert.Equal(false, await Query($"""
                    SELECT EXISTS(SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                    WHERE n.nspname='{other}' AND has_function_privilege(current_user,p.oid,'EXECUTE'))
                    """));
                var table = other switch { "identity" => "users", "community" => "servers", _ => "spaces" };
                var denied = await Assert.ThrowsAsync<PostgresException>(() => Query($"SELECT id FROM {other}.{table} LIMIT 1"));
                Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
                denied = await Assert.ThrowsAsync<PostgresException>(() => Query($"UPDATE {other}.{table} SET id=id WHERE false"));
                Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
            }
        }
    }
}
