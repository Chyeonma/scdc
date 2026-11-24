using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace SCDC.DbMigrator;

public static class ChannelProvisionMigration
{
    public const string FileName = "20261124_s2-09_channel_provisioning.sql";
    public const int Version = 2026112409;
    public static async Task<bool> ApplyAsync(string connectionString, string sql, CancellationToken ct = default)
    {
        var checksum = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var gate = new NpgsqlCommand("SELECT pg_advisory_xact_lock(1935893324,1); SET LOCAL lock_timeout='5s'", connection, transaction))
            await gate.ExecuteNonQueryAsync(ct);
        await using (var prerequisite = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM community.schema_migrations WHERE module='community' AND version=2026112307)", connection, transaction))
            if (await prerequisite.ExecuteScalarAsync(ct) is not true) throw new MigrationPreflightException("Apply S2-07 before S2-09. S2-08 is independent.");
        var applied = 0;
        foreach (var owner in new[] { "community", "messaging" })
        {
            await using var lookup = new NpgsqlCommand($"SELECT checksum FROM {owner}.schema_migrations WHERE module=@owner AND version=@v", connection, transaction);
            lookup.Parameters.AddWithValue("owner", owner); lookup.Parameters.AddWithValue("v", Version);
            if (await lookup.ExecuteScalarAsync(ct) is string previous)
            {
                if (previous != checksum) throw new MigrationPreflightException("S2-09 checksum differs from the applied migration.");
                applied++;
            }
        }
        if (applied == 1) throw new MigrationPreflightException("S2-09 ledgers disagree. No changes applied.");
        if (applied == 2) { await transaction.CommitAsync(ct); return false; }
        await using (var migrate = new NpgsqlCommand(sql, connection, transaction) { CommandTimeout = 30 }) await migrate.ExecuteNonQueryAsync(ct);
        foreach (var owner in new[] { "community", "messaging" })
        {
            await using var ledger = new NpgsqlCommand($"INSERT INTO {owner}.schema_migrations(module,version,checksum) VALUES(@owner,@v,@checksum)", connection, transaction);
            ledger.Parameters.AddWithValue("owner", owner); ledger.Parameters.AddWithValue("v", Version); ledger.Parameters.AddWithValue("checksum", checksum);
            await ledger.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct); return true;
    }
}
