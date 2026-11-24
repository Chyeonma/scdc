using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace SCDC.DbMigrator;

public static class CommunityAccessMigration
{
    public const string FileName = "20261123_s2-07_community_access.sql";
    public const int Version = 2026112307;
    public static async Task<bool> ApplyAsync(string connectionString, string sql, CancellationToken ct = default)
    {
        var checksum = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var gate = new NpgsqlCommand("SELECT pg_advisory_xact_lock(1935893324,1); SET LOCAL lock_timeout='5s'", connection, transaction))
            await gate.ExecuteNonQueryAsync(ct);
        await using (var prerequisite = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM identity.schema_migrations WHERE module='identity' AND version=2026112306)", connection, transaction))
            if (await prerequisite.ExecuteScalarAsync(ct) is not true) throw new MigrationPreflightException("Apply S2-06 before S2-07.");
        await using (var lookup = new NpgsqlCommand("SELECT checksum FROM community.schema_migrations WHERE module='community' AND version=@v", connection, transaction))
        {
            lookup.Parameters.AddWithValue("v", Version);
            if (await lookup.ExecuteScalarAsync(ct) is string existing)
            {
                if (existing != checksum) throw new MigrationPreflightException("S2-07 checksum differs from the applied migration.");
                await transaction.CommitAsync(ct);
                return false;
            }
        }
        await using (var migrate = new NpgsqlCommand(sql, connection, transaction)) await migrate.ExecuteNonQueryAsync(ct);
        await using (var ledger = new NpgsqlCommand("INSERT INTO community.schema_migrations(module,version,checksum) VALUES('community',@v,@c)", connection, transaction))
        { ledger.Parameters.AddWithValue("v", Version); ledger.Parameters.AddWithValue("c", checksum); await ledger.ExecuteNonQueryAsync(ct); }
        await transaction.CommitAsync(ct);
        return true;
    }
}
