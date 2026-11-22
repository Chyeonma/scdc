using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace SCDC.DbMigrator;

public static class SchemaBoundaryMigration
{
    public const string CommunityFile = "20261122_s2-04_drop-community-fk.sql";
    public const string MessagingFile = "20261122_s2-05_drop-messaging-fk-views.sql";
    public const int CommunityVersion = 2026112204;
    public const int MessagingVersion = 2026112205;

    public static async Task<bool> ApplyAsync(string connectionString, string filename, string sql, CancellationToken ct = default)
    {
        var (owner, version) = Path.GetFileName(filename) switch
        {
            CommunityFile => ("community", CommunityVersion),
            MessagingFile => ("messaging", MessagingVersion),
            _ => throw new MigrationPreflightException("Unsupported schema-boundary migration filename.")
        };
        var checksum = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var gate = new NpgsqlCommand("SELECT pg_advisory_xact_lock(1935893324,1); SET LOCAL lock_timeout='5s'", connection, transaction))
            await gate.ExecuteNonQueryAsync(ct);
        await using (var lookup = new NpgsqlCommand($"SELECT to_regclass('{owner}.schema_migrations') IS NOT NULL", connection, transaction))
            if (await lookup.ExecuteScalarAsync(ct) is not true)
                throw new MigrationPreflightException("Apply S2-03 before the schema-boundary migrations.");
        await using (var prerequisite = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {owner}.schema_migrations WHERE module=@owner AND version=@version)", connection, transaction))
        {
            prerequisite.Parameters.AddWithValue("owner", owner);
            prerequisite.Parameters.AddWithValue("version", ModuleDatabaseMigration.Version);
            if (await prerequisite.ExecuteScalarAsync(ct) is not true)
                throw new MigrationPreflightException("The owner's S2-03 ledger record is missing.");
        }
        await using (var lookup = new NpgsqlCommand($"SELECT checksum FROM {owner}.schema_migrations WHERE module=@owner AND version=@version", connection, transaction))
        {
            lookup.Parameters.AddWithValue("owner", owner);
            lookup.Parameters.AddWithValue("version", version);
            if (await lookup.ExecuteScalarAsync(ct) is string existing)
            {
                if (existing != checksum)
                    throw new MigrationPreflightException("Schema-boundary migration checksum differs from the applied version.");
                await transaction.CommitAsync(ct);
                return false;
            }
        }
        try
        {
            await using var migrate = new NpgsqlCommand(sql, connection, transaction) { CommandTimeout = 60 };
            await migrate.ExecuteNonQueryAsync(ct);
        }
        catch (PostgresException error) when (error.SqlState == "P0001")
        {
            // These SQL files raise fixed preflight messages; surface the reason
            // without exposing arbitrary provider errors or business data.
            throw new MigrationPreflightException(error.MessageText);
        }
        await using (var record = new NpgsqlCommand($"INSERT INTO {owner}.schema_migrations(module,version,checksum) VALUES(@owner,@version,@checksum)", connection, transaction))
        {
            record.Parameters.AddWithValue("owner", owner);
            record.Parameters.AddWithValue("version", version);
            record.Parameters.AddWithValue("checksum", checksum);
            await record.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return true;
    }
}
