using Microsoft.Extensions.Configuration;
using Npgsql;
using SCDC.Modules.Community.Infrastructure;

namespace SCDC.Modules.Community.Infrastructure;

public sealed class CommunityDatabaseReadiness(IConfiguration configuration)
{
    public async Task<bool> IsReadyAsync(CancellationToken ct)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            await using var connection = new NpgsqlConnection(ModuleDatabaseConnection.Get(configuration));
            await connection.OpenAsync(deadline.Token);
            await using var query = new NpgsqlCommand("""
                SELECT to_regclass('community.servers') IS NOT NULL
                    AND to_regclass('community.schema_migrations') IS NOT NULL
                    AND to_regclass('community.access_changes') IS NOT NULL
                    AND to_regclass('community.channel_provisions') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM pg_namespace WHERE nspname IN ('identity','messaging'))
                    AND NOT has_schema_privilege(current_user,'community','CREATE')
                    AND NOT has_schema_privilege(current_user,'public','CREATE')
                    AND has_table_privilege(current_user,'community.servers','SELECT')
                    AND NOT has_table_privilege(current_user,'community.schema_migrations','INSERT,UPDATE,DELETE')
                    AND EXISTS (SELECT 1 FROM pg_roles WHERE rolname=current_user
                        AND NOT rolsuper AND NOT rolcreatedb AND NOT rolcreaterole AND NOT rolbypassrls)
                """, connection);
            return await query.ExecuteScalarAsync(deadline.Token) is true;
        }
        catch (Exception error) when (error is NpgsqlException or OperationCanceledException or TimeoutException)
        {
            return false;
        }
    }
}
