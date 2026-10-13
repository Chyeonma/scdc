using System.Security.Cryptography;
using System.Text;
using Npgsql;
using SCDC.BuildingBlocks.Application.Text;

namespace SCDC.DbMigrator;

public sealed record LegacyServerSettings(string Visibility, string JoinMode);
public sealed class MigrationPreflightException(string message) : Exception(message);

public static class CommunityMigration
{
    public static async Task<bool> ApplyAsync(string connectionString, string sql,
        IReadOnlyDictionary<Guid, LegacyServerSettings>? map = null, CancellationToken cancellationToken = default)
    {
        map ??= new Dictionary<Guid, LegacyServerSettings>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var setup = new NpgsqlCommand("""
            SELECT pg_advisory_xact_lock(1935893324, 1);
            CREATE TABLE IF NOT EXISTS common.schema_migrations (
                module varchar(50) NOT NULL, version integer NOT NULL, checksum char(64) NOT NULL,
                applied_at timestamptz NOT NULL DEFAULT clock_timestamp(), PRIMARY KEY(module,version));
            """, connection, transaction))
            await setup.ExecuteNonQueryAsync(cancellationToken);
        var checksum = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
        await using (var lookup = new NpgsqlCommand("SELECT checksum FROM common.schema_migrations WHERE module='community' AND version=1", connection, transaction))
        {
            var existing = await lookup.ExecuteScalarAsync(cancellationToken) as string;
            if (existing is not null)
            {
                if (existing != checksum) throw new MigrationPreflightException("Migration 001 checksum differs from the applied version.");
                await transaction.CommitAsync(cancellationToken);
                return false;
            }
        }
        // Keep preflight and DDL on the same reviewed set of rows.
        await using (var tableLock = new NpgsqlCommand("LOCK TABLE community.servers,community.server_members,community.roles,community.role_permissions IN SHARE ROW EXCLUSIVE MODE", connection, transaction))
            await tableLock.ExecuteNonQueryAsync(cancellationToken);
        await PreflightAsync(connection, transaction, map, cancellationToken);
        await using (var createMap = new NpgsqlCommand("CREATE TEMP TABLE community_server_migration_map (id uuid PRIMARY KEY,visibility smallint NOT NULL,join_mode smallint NOT NULL) ON COMMIT DROP", connection, transaction))
            await createMap.ExecuteNonQueryAsync(cancellationToken);
        foreach (var (id, settings) in map)
        {
            await using var insert = new NpgsqlCommand("INSERT INTO community_server_migration_map VALUES (@id,@visibility,@mode)", connection, transaction);
            insert.Parameters.AddWithValue("id", id);
            insert.Parameters.AddWithValue("visibility", (short)(settings.Visibility == "public" ? 1 : 2));
            insert.Parameters.AddWithValue("mode", (short)(settings.JoinMode == "immediate" ? 1 : 2));
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
        await using (var migration = new NpgsqlCommand(sql, connection, transaction) { CommandTimeout = 30 })
            await migration.ExecuteNonQueryAsync(cancellationToken);
        await using (var record = new NpgsqlCommand("INSERT INTO common.schema_migrations(module,version,checksum) VALUES('community',1,@checksum)", connection, transaction))
        {
            record.Parameters.AddWithValue("checksum", checksum);
            await record.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static async Task PreflightAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        IReadOnlyDictionary<Guid, LegacyServerSettings> map, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var ids = new HashSet<Guid>();
        await using (var command = new NpgsqlCommand("""
            SELECT s.id,s.name,s.description,
                EXISTS(SELECT 1 FROM community.server_members m WHERE m.server_id=s.id AND m.user_id=s.owner_user_id AND m.status=1),
                (SELECT count(*) FROM community.roles r WHERE r.server_id=s.id AND r.is_default AND r.is_system AND r.name='@everyone')
            FROM community.servers s ORDER BY s.id
            """, connection, transaction))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                ids.Add(id);
                if (!map.TryGetValue(id, out var settings)) errors.Add($"Server {id}: missing reviewed visibility/joinMode mapping.");
                else if (settings.Visibility is not ("public" or "private") || settings.JoinMode is not ("immediate" or "approval"))
                    errors.Add($"Server {id}: invalid visibility/joinMode mapping.");
                var name = reader.GetString(1);
                if (!UnicodeTextPolicy.IsValidName(name, 2, 100) || name != UnicodeTextPolicy.TrimWhitespace(name))
                    errors.Add($"Server {id}: name requires reviewed repair.");
                if (!reader.IsDBNull(2))
                {
                    var description = reader.GetString(2);
                    if (!UnicodeTextPolicy.IsValidUnicode(description) || description.Length > 1000
                        || description != UnicodeTextPolicy.NormalizeDescription(description)) errors.Add($"Server {id}: description requires reviewed repair.");
                }
                if (!reader.GetBoolean(3)) errors.Add($"Server {id}: owner membership is not active.");
                if (reader.GetInt64(4) != 1) errors.Add($"Server {id}: default role must already be a reviewed @everyone.");
            }
        }
        foreach (var id in map.Keys.Except(ids)) errors.Add($"Server {id}: mapping references a missing server.");
        await using (var check = new NpgsqlCommand("""
            SELECT 'member status/epoch requires repair: ' || server_id || '/' || user_id
              FROM community.server_members WHERE status NOT IN (1,2)
            UNION ALL SELECT 'system/default role requires repair: ' || id FROM community.roles
              WHERE (is_default OR is_system) AND NOT (is_default AND is_system AND name='@everyone')
            UNION ALL SELECT 'role permission requires review: ' || rp.role_id FROM community.role_permissions rp
              JOIN community.roles r ON r.id=rp.role_id
              WHERE rp.permission_code NOT IN ('manage_channels','manage_invites','review_join_requests','manage_join_mode','manage_channel_access') OR r.is_default
            """, connection, transaction))
        await using (var reader = await check.ExecuteReaderAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken)) errors.Add(reader.GetString(0));
        if (errors.Count > 0) throw new MigrationPreflightException("Community migration preflight failed; no data was changed:\n" + string.Join('\n', errors));
    }
}
