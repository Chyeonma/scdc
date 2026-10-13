using System.Security.Cryptography;
using System.Text;
using Npgsql;
using NpgsqlTypes;
using SCDC.BuildingBlocks.Application.Text;

namespace SCDC.DbMigrator;

public static class CommunityRoleMigration
{
    public static async Task<bool> ApplyAsync(string connectionString, string sql, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        async Task<object?> Scalar(string query)
        {
            await using var command = new NpgsqlCommand(query,connection,transaction);
            return await command.ExecuteScalarAsync(ct);
        }
        await Scalar("SELECT pg_advisory_xact_lock(1935893324,1)");
        if (await Scalar("SELECT to_regclass('common.schema_migrations') IS NOT NULL") is not true)
            throw new MigrationPreflightException("Apply supported Community migrations 001/002 before 003.");
        if (await Scalar("SELECT checksum FROM common.schema_migrations WHERE module='community' AND version=1") as string != "b434be5e002497ef204a39c38d7cb4d0783f869804824e130779e74d8f8adab4"
            || await Scalar("SELECT checksum FROM common.schema_migrations WHERE module='community' AND version=2") as string != "fe07dcc516bfce7eedf257c8fb42d7baa615c716aba9034de9f4e9cebe40ccff")
            throw new MigrationPreflightException("Apply supported Community migrations 001/002 before 003.");
        var checksum=Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
        if (await Scalar("SELECT checksum FROM common.schema_migrations WHERE module='community' AND version=3") is string existing)
        {
            if(existing!=checksum) throw new MigrationPreflightException("Migration 003 checksum differs from the applied version.");
            await transaction.CommitAsync(ct);
            return false;
        }
        await using (var setup=new NpgsqlCommand("""
            LOCK TABLE community.servers,community.roles,community.server_members,community.member_roles,
                community.role_permissions,community.channel_role_overrides,community.channel_user_overrides IN SHARE ROW EXCLUSIVE MODE;
            CREATE TEMP TABLE community_role_migration_map(id uuid PRIMARY KEY,server_id uuid NOT NULL,name_key text COLLATE "C" NOT NULL) ON COMMIT DROP;
            """,connection,transaction)) await setup.ExecuteNonQueryAsync(ct);
        if (await Scalar("""
            SELECT EXISTS(SELECT 1 FROM community.roles WHERE NOT is_system GROUP BY server_id HAVING count(*)>20)
              OR EXISTS(SELECT 1 FROM community.role_permissions WHERE permission_code NOT IN
                ('manage_channels','manage_invites','review_join_requests','manage_join_mode','manage_channel_access'))
              OR EXISTS(SELECT 1 FROM community.channel_role_overrides WHERE permission_code<>'channel_view')
              OR EXISTS(SELECT 1 FROM community.channel_user_overrides WHERE permission_code<>'channel_view')
            """) is true) throw new MigrationPreflightException("Legacy role count or permission/override catalog requires reviewed repair; no changes committed.");
        Guid? last=null;
        while(true)
        {
            var batchRows=new List<(Guid Id,Guid Server,string Key)>(500);
            await using(var command=new NpgsqlCommand("SELECT id,server_id,name FROM community.roles WHERE(@last IS NULL OR id>@last) ORDER BY id LIMIT 500",connection,transaction))
            {
                command.Parameters.AddWithValue("last",NpgsqlDbType.Uuid,(object?)last??DBNull.Value);
                await using var rows=await command.ExecuteReaderAsync(ct);
                while(await rows.ReadAsync(ct))
                {
                    var id=rows.GetGuid(0);var name=rows.GetString(2);
                    if(!UnicodeTextPolicy.IsValidName(name,1,64)||name!=UnicodeTextPolicy.TrimWhitespace(name))
                        throw new MigrationPreflightException($"Role {id}: name requires reviewed repair; no changes committed.");
                    batchRows.Add((id,rows.GetGuid(1),UnicodeTextPolicy.NormalizeNameKey(name)));
                }
            }
            if(batchRows.Count==0)break;
            await using var batch=new NpgsqlBatch(connection,transaction);
            foreach(var row in batchRows)
            {
                var insert=new NpgsqlBatchCommand("INSERT INTO community_role_migration_map(id,server_id,name_key) VALUES(@id,@server,@key)");
                insert.Parameters.AddWithValue("id",row.Id);insert.Parameters.AddWithValue("server",row.Server);insert.Parameters.AddWithValue("key",row.Key);
                batch.BatchCommands.Add(insert);
            }
            await batch.ExecuteNonQueryAsync(ct);
            last=batchRows[^1].Id;
        }
        if(await Scalar("SELECT server_id FROM community_role_migration_map GROUP BY server_id,name_key HAVING count(*)>1 LIMIT 1") is Guid conflict)
            throw new MigrationPreflightException($"Server {conflict}: normalized role names collide; no names changed.");
        await using(var migration=new NpgsqlCommand(sql,connection,transaction){CommandTimeout=30})await migration.ExecuteNonQueryAsync(ct);
        await using(var record=new NpgsqlCommand("INSERT INTO common.schema_migrations(module,version,checksum) VALUES('community',3,@checksum)",connection,transaction))
        {record.Parameters.AddWithValue("checksum",checksum);await record.ExecuteNonQueryAsync(ct);}
        await transaction.CommitAsync(ct);
        return true;
    }
}
