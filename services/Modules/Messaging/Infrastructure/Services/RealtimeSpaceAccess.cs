using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class RealtimeSpaceAccess(MessagingDbContext dbContext) : IRealtimeSpaceAccess
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

        var space = await (
            from item in dbContext.Spaces.AsNoTracking()
            join member in dbContext.SpaceMembers.AsNoTracking() on item.Id equals member.SpaceId
            where item.Id == spaceId
                  && item.SpaceType == SpaceType.Direct
                  && item.Status != SpaceStatus.Deleted
                  && member.UserId == userId
                  && member.MembershipStatus == SpaceMembershipStatus.Active
            select new { item.Id, item.LastMessageSequence })
            .SingleOrDefaultAsync(cancellationToken);
        return space is null
            ? null
            : new RealtimeSpaceReadAccess(
                space.Id,
                (space.LastMessageSequence ?? 0).ToString(CultureInfo.InvariantCulture));
    }

    public async Task<IReadOnlyList<Guid>> GetActiveMemberIdsAsync(
        Guid spaceId,
        CancellationToken cancellationToken) => await (
            from item in dbContext.Spaces.AsNoTracking()
            join member in dbContext.SpaceMembers.AsNoTracking() on item.Id equals member.SpaceId
            where item.Id == spaceId
                  && item.SpaceType == SpaceType.Direct
                  && item.Status != SpaceStatus.Deleted
                  && member.MembershipStatus == SpaceMembershipStatus.Active
            select member.UserId)
            .ToListAsync(cancellationToken);
}
