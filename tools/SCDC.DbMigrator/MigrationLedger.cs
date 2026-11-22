using Npgsql;

namespace SCDC.DbMigrator;

internal static class MigrationLedger
{
    public static async Task<string> CommunityAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct)
    {
        await using var lookup = new NpgsqlCommand("SELECT to_regclass('community.schema_migrations') IS NOT NULL",connection,transaction);
        return await lookup.ExecuteScalarAsync(ct) is true ? "community.schema_migrations" : "common.schema_migrations";
    }
}
