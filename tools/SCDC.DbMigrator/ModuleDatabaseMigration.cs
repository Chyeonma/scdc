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
        // Older dev volumes have a pre-module Identity ledger (id/applied_at).
        // Keep that relation intact; the checksummed S2-03 SQL creates the new ledger.
        await using (var legacy = new NpgsqlCommand("SELECT to_regclass('identity.schema_migrations') IS NOT NULL",connection,transaction))
        {
            if (await legacy.ExecuteScalarAsync(ct) is true)
            {
                await using var shape = new NpgsqlCommand("""
                    SELECT count(*)=2 AND bool_and(
                        (column_name='id' AND data_type='text' AND is_nullable='NO') OR
                        (column_name='applied_at' AND data_type='timestamp with time zone' AND is_nullable='NO'))
                    FROM information_schema.columns
                    WHERE table_schema='identity' AND table_name='schema_migrations'
                    """,connection,transaction);
                if (await shape.ExecuteScalarAsync(ct) is not true)
                    throw new MigrationPreflightException("Unknown legacy Identity ledger shape; review it before S2-03.");
                await using var archive = new NpgsqlCommand("""
                    LOCK TABLE identity.schema_migrations IN ACCESS EXCLUSIVE MODE;
                    ALTER TABLE identity.schema_migrations RENAME TO legacy_schema_migrations;
                    """,connection,transaction);
                await archive.ExecuteNonQueryAsync(ct);
            }
        }
        // A legacy profile constraint still references this helper by OID.
        // Moving it preserves that constraint while allowing common to be retired.
        await using (var helper = new NpgsqlCommand("SELECT to_regprocedure('common.utf16_length_identity_legacy(text)') IS NOT NULL",connection,transaction))
        {
            if (await helper.ExecuteScalarAsync(ct) is true)
            {
                await using var move = new NpgsqlCommand("ALTER FUNCTION common.utf16_length_identity_legacy(text) SET SCHEMA identity",connection,transaction);
                await move.ExecuteNonQueryAsync(ct);
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
