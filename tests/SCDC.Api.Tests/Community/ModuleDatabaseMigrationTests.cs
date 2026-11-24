using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Identity.Infrastructure.Persistence;
using SCDC.Modules.Messaging.Infrastructure.Persistence;
using SCDC.DbMigrator;
using SCDC.Api.Tests.Infrastructure;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityMigrationTests
{
    private const string ModuleMigration = "database/postgres/migrations/20261120_s2-03_module_databases.sql";

    private async Task ModuleLegacyAsync()
    {
        // The unchanged pre-S2-03 bootstrap is embedded before this stable marker.
        var bootstrap = await ReadAsync("database/postgres/schema.sql");
        await ExecuteAsync(bootstrap[..bootstrap.IndexOf("-- S2-03 bootstrap snapshot;",StringComparison.Ordinal)] + "COMMIT;");
    }

    private async Task<string> SnapshotAsync(string table)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY to_jsonb(t)::text),'[]')::text FROM {table} t",connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    [Fact]
    public async Task ModuleDatabase_migration_preserves_every_event_audit_inbox_and_ledger_field()
    {
        await ModuleLegacyAsync();
        var seed=(await ReadAsync("database/postgres/seed.sql"))
            .Replace("identity.security_events","audit.security_events")
            .Replace("messaging.outbox_events","integration.outbox_events");
        await ExecuteAsync(seed);
        await ExecuteAsync("""
            INSERT INTO integration.outbox_events(id,event_type,aggregate_type,aggregate_id,aggregate_version,space_id,payload,
                occurred_at,available_at,published_at,attempt_count,last_error,lease_token,lease_expires_at)
            SELECT gen_random_uuid(),kind,'Example',gen_random_uuid(),7,gen_random_uuid(),'{"kept":true}',
                '2026-01-01Z','2026-01-02Z',CASE WHEN n=1 THEN '2026-01-03Z'::timestamptz END,n,'retained-error',
                CASE WHEN n=2 THEN gen_random_uuid() END,CASE WHEN n=2 THEN '2026-11-21Z'::timestamptz END
            FROM unnest(ARRAY['Identity.Registered','Community.ServerCreated.v1','Messaging.MessageChanged','MessageCreated']) WITH ORDINALITY x(kind,n);
            INSERT INTO audit.security_events(user_id,event_type,ip_address,user_agent,metadata,occurred_at)
                VALUES(gen_random_uuid(),'Test.Audit','127.0.0.2','retained-agent','{"kept":true}','2026-01-01Z');
            INSERT INTO integration.inbox_events VALUES('historical-consumer',gen_random_uuid(),'2026-01-01Z',NULL,repeat('a',64),'retry');
            INSERT INTO community.operations(actor_user_id,kind,scope_id,client_operation_id,fingerprint_version,key_id,fingerprint,resource_id)
                SELECT owner_user_id,'create_server','00000000-0000-0000-0000-000000000000',gen_random_uuid(),1,'retained-key',decode(repeat('ab',32),'hex'),id
                FROM community.servers LIMIT 1;
            INSERT INTO messaging.send_operations(space_id,author_user_id,client_message_id,message_id,fingerprint_version,key_id,fingerprint)
                SELECT space_id,author_user_id,gen_random_uuid(),id,1,'retained-key',decode(repeat('cd',32),'hex') FROM messaging.messages LIMIT 1;
            """);
        var events=await SnapshotAsync("integration.outbox_events");
        var inbox=await SnapshotAsync("integration.inbox_events");
        var audit=await SnapshotAsync("audit.security_events");
        var ledger=await SnapshotAsync("common.schema_migrations");
        var communityOperations=await SnapshotAsync("community.operations");
        var sendOperations=await SnapshotAsync("messaging.send_operations");
        var sql=await ReadAsync(ModuleMigration);
        Assert.True(await ModuleDatabaseMigration.ApplyAsync(_connectionString,sql,new Dictionary<string,string>{{"historical-consumer","messaging"}}));
        Assert.Equal(events,await SnapshotAsync("(SELECT * FROM identity.outbox_events UNION ALL SELECT * FROM community.outbox_events UNION ALL SELECT * FROM messaging.outbox_events)"));
        Assert.Equal(inbox,await SnapshotAsync("messaging.inbox_events"));
        Assert.Equal(audit,await SnapshotAsync("identity.security_events"));
        Assert.Equal(ledger,await SnapshotAsync("(SELECT * FROM community.schema_migrations WHERE version<20261120)"));
        Assert.Equal(communityOperations,await SnapshotAsync("community.operations"));
        Assert.Equal(sendOperations,await SnapshotAsync("messaging.send_operations"));
        Assert.False(await ModuleDatabaseMigration.ApplyAsync(_connectionString,sql));
        await Assert.ThrowsAsync<MigrationPreflightException>(()=>ModuleDatabaseMigration.ApplyAsync(_connectionString,sql+"\n-- changed"));
        foreach (var migration in new[] { "001-community-create-view.sql","002-community-search.sql","003-community-roles.sql","004-community-channels-access.sql" })
        {
            var old=await ReadAsync("database/postgres/migrations/"+migration);
            Assert.False(migration[..3] switch {
                "001"=>await CommunityMigration.ApplyAsync(_connectionString,old),
                "002"=>await CommunitySearchMigration.ApplyAsync(_connectionString,old),
                "003"=>await CommunityRoleMigration.ApplyAsync(_connectionString,old),
                _=>await CommunityChannelMigration.ApplyAsync(_connectionString,old)});
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ModuleDatabase_archives_legacy_identity_ledger_and_rejects_unknown_shape(bool unknown)
    {
        await ModuleLegacyAsync();
        await ExecuteAsync("""
            CREATE TABLE identity.schema_migrations(id text PRIMARY KEY,applied_at timestamptz NOT NULL DEFAULT clock_timestamp());
            INSERT INTO identity.schema_migrations VALUES('001_identity_email','2026-10-05 12:32:54Z');
            CREATE FUNCTION common.utf16_length_identity_legacy(value text) RETURNS integer
            LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT AS $$
                SELECT coalesce(sum(CASE WHEN ascii(substr(value, position, 1)) > 65535 THEN 2 ELSE 1 END),0)::integer
                FROM generate_series(1,char_length(value)) AS position
            $$;
            ALTER TABLE identity.user_profiles ADD CONSTRAINT ck_legacy_profile_utf16
                CHECK(common.utf16_length_identity_legacy(display_name) BETWEEN 1 AND 64);
            """);
        var before=await SnapshotAsync("identity.schema_migrations");
        var helperOid=await BoundaryScalarAsync("SELECT 'common.utf16_length_identity_legacy(text)'::regprocedure::oid::bigint");
        var constraint=await SnapshotAsync("(SELECT oid,conname FROM pg_constraint WHERE conname='ck_legacy_profile_utf16')");
        var sql=await ReadAsync(ModuleMigration);
        if(unknown)
        {
            await ExecuteAsync("ALTER TABLE identity.schema_migrations ADD COLUMN unexpected integer");
            await Assert.ThrowsAsync<MigrationPreflightException>(()=>ModuleDatabaseMigration.ApplyAsync(_connectionString,sql));
            Assert.Equal(before,await SnapshotAsync("(SELECT id,applied_at FROM identity.schema_migrations)"));
            Assert.Equal("[]",await SnapshotAsync("(SELECT to_regclass('identity.legacy_schema_migrations') AS archived WHERE to_regclass('identity.legacy_schema_migrations') IS NOT NULL)"));
            return;
        }
        Assert.True(await ModuleDatabaseMigration.ApplyAsync(_connectionString,sql));
        Assert.Equal(before,await SnapshotAsync("identity.legacy_schema_migrations"));
        Assert.Equal(helperOid,await BoundaryScalarAsync("SELECT 'identity.utf16_length_identity_legacy(text)'::regprocedure::oid::bigint"));
        Assert.Equal(constraint,await SnapshotAsync("(SELECT oid,conname FROM pg_constraint WHERE conname='ck_legacy_profile_utf16')"));
        Assert.Equal(2,await BoundaryScalarAsync("SELECT identity.utf16_length_identity_legacy('🙂')::bigint"));
        Assert.False(await ModuleDatabaseMigration.ApplyAsync(_connectionString,sql));
        Assert.Equal(before,await SnapshotAsync("identity.legacy_schema_migrations"));
    }

    [Theory]
    [InlineData("event")]
    [InlineData("inbox")]
    [InlineData("object")]
    public async Task ModuleDatabase_preflight_unknown_owner_rolls_back_everything(string kind)
    {
        await ModuleLegacyAsync();
        await ExecuteAsync(kind switch {
            "event"=>"INSERT INTO integration.outbox_events(event_type,aggregate_type,aggregate_id,payload) VALUES('Unknown.Event','Unknown',gen_random_uuid(),'{}')",
            "inbox"=>"INSERT INTO integration.inbox_events(consumer_name,event_id) VALUES('unknown-consumer',gen_random_uuid())",
            _=>"CREATE TABLE integration.unreviewed_data(id int)"});
        var table="integration."+(kind=="inbox"?"inbox_events":"outbox_events");
        var before=await SnapshotAsync(table);
        var sql=await ReadAsync(ModuleMigration);
        await Assert.ThrowsAsync<PostgresException>(()=>ModuleDatabaseMigration.ApplyAsync(_connectionString,sql));
        Assert.Equal(before,await SnapshotAsync(table));
        await using var connection=new NpgsqlConnection(_connectionString);await connection.OpenAsync();
        await using var command=new NpgsqlCommand("SELECT to_regclass('identity.outbox_events') IS NULL AND to_regclass('audit.security_events') IS NOT NULL",connection);
        Assert.True((bool)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task ModuleDatabase_bootstrap_matches_migration_checksum_and_supports_all_owner_ledgers()
    {
        var sql=await ReadAsync(ModuleMigration);
        var bootstrap=await ReadAsync("database/postgres/schema.sql");
        Assert.Contains(sql,bootstrap);
        await ExecuteAsync(bootstrap);
        Assert.False(await ModuleDatabaseMigration.ApplyAsync(_connectionString,sql));
    }

    [Fact]
    public async Task ModuleDatabase_scope_keys_are_canonical_and_epoch_cannot_reset_or_overflow()
    {
        await ExecuteAsync(await ReadAsync("database/postgres/schema.sql"));
        foreach(var owner in new[]{"community","messaging"})
        {
            await ExecuteAsync($"""
                INSERT INTO {owner}.access_fences(authority,scope_kind,user_id,min_epoch) VALUES(1,1,gen_random_uuid(),1);
                INSERT INTO {owner}.access_fences(authority,scope_kind,user_id,session_id,min_epoch) VALUES(1,2,gen_random_uuid(),gen_random_uuid(),1);
                INSERT INTO {owner}.access_fences(authority,scope_kind,user_id,server_id,membership_id,min_epoch) VALUES(2,3,gen_random_uuid(),gen_random_uuid(),gen_random_uuid(),1);
                INSERT INTO {owner}.access_fences(authority,scope_kind,user_id,server_id,channel_id,membership_id,min_epoch) VALUES(2,4,gen_random_uuid(),gen_random_uuid(),gen_random_uuid(),gen_random_uuid(),1);
                INSERT INTO {owner}.access_fences(authority,scope_kind,user_id,server_id,membership_id,permission_code,min_epoch) VALUES(2,5,gen_random_uuid(),gen_random_uuid(),gen_random_uuid(),'manage_channels',1);
                INSERT INTO {owner}.access_fences(authority,scope_kind,server_id,channel_id,min_epoch) VALUES(2,6,gen_random_uuid(),gen_random_uuid(),1);
                UPDATE {owner}.access_fences SET min_epoch=9223372036854775807;
                """);
            foreach(var invalid in new[]{
                $"UPDATE {owner}.access_fences SET min_epoch=min_epoch+1",
                $"UPDATE {owner}.access_fences SET min_epoch=0",
                $"UPDATE {owner}.access_fences SET session_id=NULL",
                $"INSERT INTO {owner}.access_fences(authority,scope_kind,user_id,min_epoch) VALUES(2,1,gen_random_uuid(),1)"})
                await Assert.ThrowsAsync<PostgresException>(()=>ExecuteAsync(invalid));
        }
    }
}

public sealed class ModuleDatabasePermissionsTests
{
    [Theory]
    [InlineData("",20)]
    [InlineData(";MaxPoolSize=45",45)]
    [InlineData(";Maximum Pool Size=100",100)]
    public void ModuleDatabase_pool_default_respects_explicit_overrides(string suffix,int expected)
    {
        var configuration=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["ConnectionStrings:Identity"]="Host=localhost;Username=scdc_identity"+suffix }).Build();
        var value=SCDC.BuildingBlocks.Infrastructure.ModuleDatabaseConnection.Get(configuration,"Identity");
        Assert.Equal(expected,new NpgsqlConnectionStringBuilder(value).MaxPoolSize);
    }

    [Fact]
    public async Task ModuleDatabase_sessions_use_three_limited_roles_and_enforce_fences_and_receipts()
    {
        await using var factory=new SCDCWebApplicationFactory();
        var configuration=factory.Services.GetRequiredService<IConfiguration>();
        foreach(var owner in new[]{"Identity","Community","Messaging"})
        {
            await using var scope=factory.Services.CreateAsyncScope();
            var connection=owner switch
            {
                "Identity" => (NpgsqlConnection)scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.GetDbConnection(),
                "Messaging" => (NpgsqlConnection)scope.ServiceProvider.GetRequiredService<MessagingSession>().Connection,
                _ => scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>().CreateConnection()
            };
            await using var ownedConnection=owner=="Community"?connection:null;
            await connection.OpenAsync();
            async Task<object?> Query(string sql) { await using var c=new NpgsqlCommand(sql,connection);return await c.ExecuteScalarAsync(); }
            Assert.Equal("scdc_"+owner.ToLowerInvariant(),await Query("SELECT current_user"));
            Assert.False((bool)(await Query("SELECT rolsuper OR rolcreatedb OR rolcreaterole OR rolbypassrls FROM pg_roles WHERE rolname=current_user"))!);
            await Assert.ThrowsAsync<PostgresException>(()=>Query($"SELECT * FROM {owner.ToLowerInvariant()}.schema_migrations"));
            await Assert.ThrowsAsync<PostgresException>(()=>Query($"CREATE TABLE {owner.ToLowerInvariant()}.forbidden(id int)"));
            await Assert.ThrowsAsync<PostgresException>(()=>Query("SELECT * FROM identity.password_credentials"+ (owner=="Identity"?" WHERE 1=0; SELECT * FROM community.operations":"")));
            if(owner=="Identity") continue;
            if(owner=="Messaging")
                await Assert.ThrowsAsync<PostgresException>(()=>Query("SELECT * FROM messaging.v_space_overview"));
            var schema=owner.ToLowerInvariant();var user=Guid.NewGuid();var operation=Guid.NewGuid();var batch=Guid.NewGuid();
            try
            {
                await Query($"INSERT INTO {schema}.access_fences(authority,scope_kind,user_id,min_epoch) VALUES(1,1,'{user}',2)");
                await Query($"UPDATE {schema}.access_fences SET min_epoch=3 WHERE user_id='{user}'");
                await Assert.ThrowsAsync<PostgresException>(()=>Query($"UPDATE {schema}.access_fences SET min_epoch=1 WHERE user_id='{user}'"));
                await Assert.ThrowsAsync<PostgresException>(()=>Query($"UPDATE {schema}.access_fences SET user_id=gen_random_uuid() WHERE user_id='{user}'"));
                await Assert.ThrowsAsync<PostgresException>(()=>Query($"DELETE FROM {schema}.access_fences WHERE user_id='{user}'"));
                await Assert.ThrowsAsync<PostgresException>(()=>Query($"INSERT INTO {schema}.access_fences(authority,scope_kind,user_id,min_epoch) VALUES(1,1,'{user}',4)"));
                await Assert.ThrowsAsync<PostgresException>(()=>Query($"INSERT INTO {schema}.access_fences(authority,scope_kind,min_epoch) VALUES(1,1,1)"));
                await Assert.ThrowsAsync<PostgresException>(()=>Query($"INSERT INTO {schema}.access_fences(authority,scope_kind,user_id,session_id,min_epoch) VALUES(1,1,gen_random_uuid(),gen_random_uuid(),1)"));
                await Query($"INSERT INTO {schema}.fence_receipts(authority,operation_id,batch_id,command_fingerprint,routing_generation,installed_scopes) VALUES(1,'{operation}','{batch}',repeat('a',64),1,'[{{\"epoch\":3}}]')");
                await Assert.ThrowsAsync<PostgresException>(()=>Query($"UPDATE {schema}.fence_receipts SET routing_generation=2 WHERE operation_id='{operation}'"));
                await Assert.ThrowsAsync<PostgresException>(()=>Query($"DELETE FROM {schema}.fence_receipts WHERE operation_id='{operation}'"));
            }
            finally
            {
                await using var admin=new NpgsqlConnection(configuration.GetConnectionString("TestDatabase"));await admin.OpenAsync();
                await using var cleanup=new NpgsqlCommand($"DELETE FROM {schema}.access_fences WHERE user_id='{user}'; DELETE FROM {schema}.fence_receipts WHERE operation_id='{operation}'",admin);
                await cleanup.ExecuteNonQueryAsync();
            }
        }
    }
}
