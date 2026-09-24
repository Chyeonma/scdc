using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class RealtimeSpaceAccess(
    MessagingDbContext dbContext,
    SpaceMessageAccess spaceAccess,
    SCDC.Contracts.Community.IChannelAccessChecker channelAccess) : IRealtimeSpaceAccess
{
    public async Task<RealtimeSpaceReadAccess?> GetReadAccessAsync(
        Guid userId,
        Guid spaceId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || spaceId == Guid.Empty)
        {
            return null;
        }

        var space = await dbContext.Spaces.AsNoTracking().SingleOrDefaultAsync(item => item.Id == spaceId, cancellationToken);
        return space is null || !(await spaceAccess.CheckAsync(userId, space, cancellationToken)).CanRead
            ? null
            : new RealtimeSpaceReadAccess(
                space.Id,
                (space.LastMessageSequence ?? 0).ToString(CultureInfo.InvariantCulture));
    }

    public async Task<IReadOnlyList<Guid>> GetActiveMemberIdsAsync(
        Guid spaceId,
        CancellationToken cancellationToken)
    {
        var isChannel = await dbContext.Spaces.AsNoTracking().AnyAsync(x => x.Id == spaceId && x.SpaceType == SpaceType.Channel && x.Status != SpaceStatus.Deleted, cancellationToken);
        if (isChannel) return await channelAccess.ListReadableMemberIdsAsync(spaceId, cancellationToken);
        return await (
            from item in dbContext.Spaces.AsNoTracking()
            join member in dbContext.SpaceMembers.AsNoTracking() on item.Id equals member.SpaceId
            where item.Id == spaceId
                  && (item.SpaceType == SpaceType.Direct || item.SpaceType == SpaceType.Group)
                  && item.Status != SpaceStatus.Deleted
                  && member.MembershipStatus == SpaceMembershipStatus.Active
            select member.UserId)
            .ToListAsync(cancellationToken);
    }
}
