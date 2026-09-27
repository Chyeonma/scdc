using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Messaging;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Moderation.Infrastructure;

internal sealed class PlatformReviewAccess(ModerationDbContext db, IUserDirectory users) : IPlatformReviewAccess
{
    public async Task<bool> IsReviewerAsync(Guid userId, CancellationToken cancellationToken) =>
        userId != Guid.Empty && await users.FindByIdAsync(userId, cancellationToken) is not null
            && await db.Reviewers.AsNoTracking()
            .AnyAsync(x => x.UserId == userId, cancellationToken);
}
