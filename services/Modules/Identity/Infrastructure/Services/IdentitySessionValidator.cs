using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Identity;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Modules.Identity.Infrastructure.Services;

internal sealed class IdentitySessionValidator(
    IdentityDbContext dbContext,
    TimeProvider timeProvider) : IIdentitySessionValidator
{
    public async Task<IdentitySessionValidation> ValidateAsync(
        Guid userId,
        Guid sessionId,
        Guid securityStamp,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || sessionId == Guid.Empty || securityStamp == Guid.Empty)
        {
            return IdentitySessionValidation.Invalid;
        }

        var now = timeProvider.GetUtcNow();
        var isValid = await dbContext.AuthSessions
            .AsNoTracking()
            .AnyAsync(session => session.Id == sessionId
                && session.UserId == userId
                && session.RevokedAt == null
                && session.ExpiresAt > now
                && session.User.Status == UserStatus.Active
                && session.User.SecurityState != null
                && session.User.SecurityState.SecurityStamp == securityStamp,
                cancellationToken);
        return isValid ? IdentitySessionValidation.Valid : IdentitySessionValidation.Invalid;
    }
}
