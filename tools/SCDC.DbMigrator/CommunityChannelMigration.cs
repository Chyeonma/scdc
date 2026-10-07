using System.Security.Cryptography;
using System.Text;
using Npgsql;
using NpgsqlTypes;
using SCDC.BuildingBlocks.Application.Text;

namespace SCDC.DbMigrator;

public sealed record LegacyChannelSettings(string Kind, string DefaultView);

public static class CommunityChannelMigration
{
    public static async Task<bool> ApplyAsync(string connectionString, string sql,
        IReadOnlyDictionary<Guid, LegacyChannelSettings>? reviewed = null, CancellationToken ct = default)
    {
        reviewed ??= new Dictionary<Guid, LegacyChannelSettings>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        async Task<object?> Scalar(string query)
        {
            await using var command = new NpgsqlCommand(query, connection, transaction);
            return await command.ExecuteScalarAsync(ct);
        }
        await Scalar("SELECT pg_advisory_xact_lock(1935893324,1)");
        if (await Scalar("SELECT to_regclass('common.schema_migrations') IS NOT NULL") is not true)
            throw new MigrationPreflightException("Apply Community migrations 001–003 before 004.");
        var checksums = new[] { "b434be5e002497ef204a39c38d7cb4d0783f869804824e130779e74d8f8adab4",
            "fe07dcc516bfce7eedf257c8fb42d7baa615c716aba9034de9f4e9cebe40ccff",
            "9c109521897297c26a2f1b920194b8ef725b2828be98d1ffb3134f8896e1069d" };
        for (var index = 0; index < checksums.Length; index++)
            if (await Scalar($"SELECT checksum FROM common.schema_migrations WHERE module='community' AND version={index + 1}") as string != checksums[index])
                throw new MigrationPreflightException("Apply supported Community migrations 001–003 before 004.");
        var checksum = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
        if (await Scalar("SELECT checksum FROM common.schema_migrations WHERE module='community' AND version=4") is string existing)
        {
            if (existing != checksum) throw new MigrationPreflightException("Migration 004 checksum differs from the applied version.");
            await transaction.CommitAsync(ct); return false;
        }
        await using (var setup = new NpgsqlCommand("""
            LOCK TABLE community.servers,community.channels,community.channel_role_overrides,
                community.channel_user_overrides,messaging.spaces IN SHARE ROW EXCLUSIVE MODE;
            CREATE TEMP TABLE community_channel_migration_map(id uuid PRIMARY KEY,server_id uuid NOT NULL,
                name_key text COLLATE "C" NOT NULL,kind smallint NOT NULL,default_view smallint NOT NULL,active boolean NOT NULL) ON COMMIT DROP;
            """, connection, transaction)) await setup.ExecuteNonQueryAsync(ct);
        Guid? last = null;
        var seen = new HashSet<Guid>();
        while (true)
        {
            var batchRows = new List<(Guid Id, Guid Server, string Key, short Kind, short View, bool Active)>(500);
            await using (var command = new NpgsqlCommand("""
                SELECT c.space_id,c.server_id,c.name,c.topic,c.visibility,s.status,s.deleted_at
                FROM community.channels c JOIN messaging.spaces s ON s.id=c.space_id
                WHERE(@last IS NULL OR c.space_id>@last) ORDER BY c.space_id LIMIT 500
                """, connection, transaction))
            {
                command.Parameters.AddWithValue("last", NpgsqlDbType.Uuid, (object?)last ?? DBNull.Value);
                await using var rows = await command.ExecuteReaderAsync(ct);
                while (await rows.ReadAsync(ct))
                {
                    var id = rows.GetGuid(0); var name = rows.GetString(2);
                    if (!reviewed.TryGetValue(id, out var settings) || settings.Kind is not ("text" or "voice") || settings.DefaultView is not ("allow" or "deny"))
                        throw new MigrationPreflightException($"Channel {id}: reviewed kind/defaultView mapping is required.");
                    if (!UnicodeTextPolicy.IsValidName(name, 1, 100) || name != UnicodeTextPolicy.TrimWhitespace(name)
                        || !rows.IsDBNull(3) && (!UnicodeTextPolicy.IsValidUnicode(rows.GetString(3)) || rows.GetString(3).Length > 1000)
                        || rows.GetInt16(4) == 3 || rows.GetInt16(5) == 2 || rows.GetInt16(5) == 3 && rows.IsDBNull(6))
                        throw new MigrationPreflightException($"Channel {id}: legacy name/topic/read-only/space state requires reviewed repair; no changes committed.");
                    seen.Add(id);
                    batchRows.Add((id, rows.GetGuid(1), UnicodeTextPolicy.NormalizeNameKey(name),
                        (short)(settings.Kind == "text" ? 1 : 2), (short)(settings.DefaultView == "allow" ? 1 : 2), rows.GetInt16(5) == 1));
                }
            }
            if (batchRows.Count == 0) break;
            await using var batch = new NpgsqlBatch(connection, transaction);
            foreach (var row in batchRows)
            {
                var insert = new NpgsqlBatchCommand("INSERT INTO community_channel_migration_map VALUES(@id,@server,@key,@kind,@view,@active)");
                insert.Parameters.AddWithValue("id", row.Id); insert.Parameters.AddWithValue("server", row.Server);
                insert.Parameters.AddWithValue("key", row.Key); insert.Parameters.AddWithValue("kind", row.Kind);
                insert.Parameters.AddWithValue("view", row.View); insert.Parameters.AddWithValue("active", row.Active);
                batch.BatchCommands.Add(insert);
            }
            await batch.ExecuteNonQueryAsync(ct); last = batchRows[^1].Id;
        }
        if (reviewed.Keys.Any(id => !seen.Contains(id))) throw new MigrationPreflightException("Channel mapping contains an unknown ID.");
        if (await Scalar("SELECT server_id FROM community_channel_migration_map WHERE active GROUP BY server_id,name_key HAVING count(*)>1 LIMIT 1") is Guid conflict)
            throw new MigrationPreflightException($"Server {conflict}: channel name keys collide; no names changed.");
        await using (var migration = new NpgsqlCommand(sql, connection, transaction) { CommandTimeout = 30 }) await migration.ExecuteNonQueryAsync(ct);
        await using (var record = new NpgsqlCommand("INSERT INTO common.schema_migrations(module,version,checksum) VALUES('community',4,@checksum)", connection, transaction))
        { record.Parameters.AddWithValue("checksum", checksum); await record.ExecuteNonQueryAsync(ct); }
        await transaction.CommitAsync(ct); return true;
    }
}
