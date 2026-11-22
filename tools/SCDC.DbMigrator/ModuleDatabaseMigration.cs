using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace SCDC.DbMigrator;

public static class ModuleDatabaseMigration
{
    public const int Version = 20261120;
    public static async Task<bool> ApplyAsync(string connectionString, string sql,
        IReadOnlyDictionary<string, string>? inboxOwners = null, CancellationToken ct = default)
    {
        var owners = new[] { "identity", "community", "messaging" };
        inboxOwners ??= new Dictionary<string, string>();
        if (inboxOwners.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || !owners.Contains(pair.Value)))
            throw new MigrationPreflightException("Inbox map values must be identity, community or messaging.");
        var checksum = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var gate = new NpgsqlCommand("SELECT pg_advisory_xact_lock(1935893324,1); SET LOCAL lock_timeout='5s'",connection,transaction))
            await gate.ExecuteNonQueryAsync(ct);
        await using (var lookup = new NpgsqlCommand("SELECT to_regclass('community.schema_migrations') IS NOT NULL",connection,transaction))
        {
            if (await lookup.ExecuteScalarAsync(ct) is true)
            {
                foreach (var owner in owners)
                {
                    await using var record = new NpgsqlCommand($"SELECT checksum FROM {owner}.schema_migrations WHERE module=@owner AND version=@version",connection,transaction);
                    record.Parameters.AddWithValue("owner",owner);
                    record.Parameters.AddWithValue("version",Version);
                    if (await record.ExecuteScalarAsync(ct) as string != checksum)
                        throw new MigrationPreflightException("S2-03 ledger is incomplete or checksum differs from the applied migration.");
                }
                await transaction.CommitAsync(ct);
                return false;
            }
        }
        await using (var map = new NpgsqlCommand("CREATE TEMP TABLE module_inbox_map(consumer_name text PRIMARY KEY,owner text NOT NULL CHECK(owner IN ('identity','community','messaging'))) ON COMMIT DROP",connection,transaction))
            await map.ExecuteNonQueryAsync(ct);
        foreach (var (consumer,owner) in inboxOwners)
        {
            await using var insert = new NpgsqlCommand("INSERT INTO module_inbox_map VALUES(@consumer,@owner)",connection,transaction);
            insert.Parameters.AddWithValue("consumer",consumer); insert.Parameters.AddWithValue("owner",owner);
            await insert.ExecuteNonQueryAsync(ct);
        }
        await using (var migrate = new NpgsqlCommand(sql,connection,transaction) { CommandTimeout=60 })
            await migrate.ExecuteNonQueryAsync(ct);
        foreach (var owner in owners)
        {
            await using var record = new NpgsqlCommand($"INSERT INTO {owner}.schema_migrations(module,version,checksum) VALUES(@owner,@version,@checksum)",connection,transaction);
            record.Parameters.AddWithValue("owner",owner); record.Parameters.AddWithValue("version",Version); record.Parameters.AddWithValue("checksum",checksum);
            await record.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return true;
    }
}
