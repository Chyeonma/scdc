using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace SCDC.DbMigrator;

/// <summary>Owner-only upgrade for the extracted Identity database or isolated component fixtures.</summary>
public static class IdentityEmailMigration
{
    public const string FileName = "20261126_e4-01_email_worker.sql";
    public const int Version = 2026112601;

    public static async Task<bool> ApplyAsync(string connectionString, string sql, CancellationToken ct = default)
    {
        var checksum = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var gate = new NpgsqlCommand("SET LOCAL lock_timeout='5s'; SELECT pg_advisory_xact_lock(1935893324,1)", connection, transaction))
            await gate.ExecuteNonQueryAsync(ct);
        await using (var prerequisite = new NpgsqlCommand("SELECT to_regclass('identity.access_changes') IS NOT NULL", connection, transaction))
            if (await prerequisite.ExecuteScalarAsync(ct) is not true)
                throw new MigrationPreflightException("Identity session/revocation baseline is required before E4-01.");
        await using (var lookup = new NpgsqlCommand("SELECT checksum FROM identity.schema_migrations WHERE module='identity' AND version=@v", connection, transaction))
        {
            lookup.Parameters.AddWithValue("v", Version);
            if (await lookup.ExecuteScalarAsync(ct) is string existing)
            {
                if (existing != checksum) throw new MigrationPreflightException("E4-01 checksum differs from the applied migration.");
                await transaction.CommitAsync(ct);
                return false;
            }
        }
        await using (var migrate = new NpgsqlCommand(sql, connection, transaction))
            await migrate.ExecuteNonQueryAsync(ct);
        await using (var ledger = new NpgsqlCommand("INSERT INTO identity.schema_migrations(module,version,checksum) VALUES('identity',@v,@c)", connection, transaction))
        {
            ledger.Parameters.AddWithValue("v", Version);
            ledger.Parameters.AddWithValue("c", checksum);
            await ledger.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return true;
    }
}
