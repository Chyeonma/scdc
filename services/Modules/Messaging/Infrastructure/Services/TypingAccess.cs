using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class TypingAccess(
    MessagingDbContext dbContext,
    SpaceMessageAccess spaceAccess,
    IUserDirectory userDirectory) : ITypingAccess
{
    public async Task<bool> CanReadAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || spaceId == Guid.Empty) return false;
        var space = await dbContext.Spaces.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == spaceId && item.Status != SpaceStatus.Deleted, cancellationToken);
        return space is not null && (await spaceAccess.CheckAsync(userId, space, cancellationToken)).CanRead;
    }

    public async Task<bool> CanSendAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || spaceId == Guid.Empty) return false;
        var space = await dbContext.Spaces.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == spaceId && item.Status == SpaceStatus.Active, cancellationToken);
        if (space is null || !(await spaceAccess.CheckAsync(userId, space, cancellationToken)).CanSend)
            return false;

        if (space.SpaceType != SpaceType.Direct) return true;
        var direct = await dbContext.DirectConversations.AsNoTracking().SingleOrDefaultAsync(
            item => item.SpaceId == spaceId, cancellationToken);
        if (direct is null) return false;
        var peerId = direct.UserLowId == userId ? direct.UserHighId : direct.UserLowId;
        return await userDirectory.FindByIdAsync(peerId, cancellationToken) is not null
               && !await dbContext.UserBlocks.AsNoTracking().AnyAsync(
                   block => block.BlockerUserId == userId && block.BlockedUserId == peerId
                            || block.BlockerUserId == peerId && block.BlockedUserId == userId,
                   cancellationToken);
    }
}
