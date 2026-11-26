using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Identity;
using SCDC.Modules.Identity.Application;
using SCDC.Modules.Identity.Infrastructure.Persistence;
using Npgsql;

namespace SCDC.Modules.Identity.Infrastructure.Services;

internal sealed class IdentityReadiness(IdentityDbContext database, ISessionStoreReadiness sessions) : IIdentityReadiness
{
    public async Task<bool> IsReadyAsync(CancellationToken ct)
    {
        try
        {
            if (!await database.Database.CanConnectAsync(ct)) return false;
            var migrated = await database.Database.SqlQuery<bool>($"""
                SELECT to_regclass('identity.account_token_policies') IS NOT NULL
                    AND to_regclass('identity.email_deliveries') IS NOT NULL AS "Value"
                """).SingleAsync(ct);
            return migrated && await sessions.IsReadyAsync(ct);
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException) { return false; }
    }
}
