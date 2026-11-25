using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Identity;
using SCDC.Modules.Identity.Application;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Modules.Identity.Infrastructure.Services;

internal sealed class IdentityReadiness(IdentityDbContext database, ISessionStoreReadiness sessions) : IIdentityReadiness
{
    public async Task<bool> IsReadyAsync(CancellationToken ct) =>
        await database.Database.CanConnectAsync(ct) && await sessions.IsReadyAsync(ct);
}
