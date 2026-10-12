using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Identity;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Modules.Identity.Infrastructure.Services;

internal sealed class AuthenticatedSessionReader(IdentityDbContext db, TimeProvider clock) : IAuthenticatedSessionReader
{
    public async Task<SessionAccess> ReadAsync(AuthenticatedSession actor, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        if (actor.AccessExpiresAt <= now) return new(false, "session_expired");
        var row = await db.AuthSessions.AsNoTracking().Where(s => s.Id == actor.SessionId && s.UserId == actor.UserId)
            .Select(s => new { s.RevokedAt, s.ExpiresAt, s.User.Status, s.User.DeletedAt,
                Stamp = s.User.SecurityState == null ? (Guid?)null : s.User.SecurityState.SecurityStamp,
                Verified = s.User.Emails.Any(e => e.IsPrimary && e.VerifiedAt != null) }).SingleOrDefaultAsync(cancellationToken);
        if (row is null || row.RevokedAt != null || row.Stamp != actor.SecurityStamp) return new(false, "session_revoked");
        if (row.ExpiresAt <= now) return new(false, "session_expired");
        return row.Status == UserStatus.Active && row.DeletedAt == null && row.Verified
            ? new(true) : new(false, "account_unavailable");
    }
}
