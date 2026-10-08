using System.Security.Cryptography;
using System.Text;
using Npgsql;
using NpgsqlTypes;
using SCDC.BuildingBlocks.Application.Text;

namespace SCDC.DbMigrator;

public static class CommunitySearchMigration
{
    private const string BaselineChecksum = "b434be5e002497ef204a39c38d7cb4d0783f869804824e130779e74d8f8adab4";

    public static async Task<bool> ApplyAsync(string connectionString, string sql, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var setup = new NpgsqlCommand("""
            SELECT pg_advisory_xact_lock(1935893324, 1);
            CREATE TABLE IF NOT EXISTS common.schema_migrations (
                module varchar(50) NOT NULL, version integer NOT NULL, checksum char(64) NOT NULL,
                applied_at timestamptz NOT NULL DEFAULT clock_timestamp(), PRIMARY KEY(module,version));
            """, connection, transaction))
            await setup.ExecuteNonQueryAsync(ct);
        await using (var baseline = new NpgsqlCommand("SELECT checksum FROM common.schema_migrations WHERE module='community' AND version=1", connection, transaction))
            if (await baseline.ExecuteScalarAsync(ct) as string != BaselineChecksum)
                throw new MigrationPreflightException("Apply the supported Community migration 001 before migration 002.");
        var checksum = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
        await using (var lookup = new NpgsqlCommand("SELECT checksum FROM common.schema_migrations WHERE module='community' AND version=2", connection, transaction))
        {
            var existing = await lookup.ExecuteScalarAsync(ct) as string;
            if (existing is not null)
            {
                if (existing != checksum) throw new MigrationPreflightException("Migration 002 checksum differs from the applied version.");
                await transaction.CommitAsync(ct);
                return false;
            }
        }
        await using (var setup = new NpgsqlCommand("""
            LOCK TABLE community.servers IN SHARE ROW EXCLUSIVE MODE;
            CREATE TEMP TABLE community_search_migration_map(id uuid PRIMARY KEY,search_name text NOT NULL) ON COMMIT DROP;
            """, connection, transaction))
            await setup.ExecuteNonQueryAsync(ct);

        // Keep application memory bounded even when upgrading a large existing database.
        Guid? last = null;
        while (true)
        {
            var rows = new List<(Guid Id, string Key)>(500);
            await using (var query = new NpgsqlCommand("SELECT id,name FROM community.servers WHERE (@last IS NULL OR id>@last) ORDER BY id LIMIT 500", connection, transaction))
            {
                query.Parameters.AddWithValue("last", NpgsqlDbType.Uuid, (object?)last ?? DBNull.Value);
                await using var reader = await query.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var id = reader.GetGuid(0);
                    var name = reader.GetString(1);
                    if (!UnicodeTextPolicy.IsValidName(name, 2, 100) || name != UnicodeTextPolicy.TrimWhitespace(name))
                        throw new MigrationPreflightException($"Server {id}: name requires reviewed repair; no changes committed.");
                    rows.Add((id, UnicodeTextPolicy.NormalizeNameKey(name)));
                }
            }
            if (rows.Count == 0) break;
            await using var batch = new NpgsqlBatch(connection, transaction);
            foreach (var row in rows)
            {
                var insert = new NpgsqlBatchCommand("INSERT INTO community_search_migration_map(id,search_name) VALUES(@id,@key)");
                insert.Parameters.AddWithValue("id", row.Id);
                insert.Parameters.AddWithValue("key", row.Key);
                batch.BatchCommands.Add(insert);
            }
            await batch.ExecuteNonQueryAsync(ct);
            last = rows[^1].Id;
        }
        await using (var migration = new NpgsqlCommand(sql, connection, transaction) { CommandTimeout = 30 })
            await migration.ExecuteNonQueryAsync(ct);
        await using (var record = new NpgsqlCommand("INSERT INTO common.schema_migrations(module,version,checksum) VALUES('community',2,@checksum)", connection, transaction))
        {
            record.Parameters.AddWithValue("checksum", checksum);
            await record.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return true;
    }
}
