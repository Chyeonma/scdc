using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Identity;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Modules.Identity.Infrastructure.Services;

internal sealed class HistoricalUserSummaryReader(IdentityDbContext db) : IHistoricalUserSummaryReader
{
    public async Task<IReadOnlyDictionary<Guid, UserSummary>> ReadAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var rows = await db.Users.AsNoTracking().Where(u => ids.Contains(u.Id) && u.Profile != null)
            .Select(u => new { u.Id, u.Username, u.Profile!.DisplayName,
                Available = u.Status == UserStatus.Active && u.DeletedAt == null
                    && u.Emails.Any(e => e.IsPrimary && e.VerifiedAt != null) })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(u => u.Id,
            u => new UserSummary(u.Id, u.Username, u.DisplayName, u.Available ? null : "unavailable"));
    }
}
