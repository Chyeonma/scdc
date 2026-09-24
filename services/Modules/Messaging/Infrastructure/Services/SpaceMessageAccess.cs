using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Community;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

/// <summary>Current read/send rights for the shared message and realtime paths.</summary>
internal sealed class SpaceMessageAccess(MessagingDbContext dbContext, IChannelAccessChecker channelAccess)
{
    public async Task<ChannelAccessDecision> CheckAsync(
        Guid userId,
        ChatSpace space,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || space.Status == SpaceStatus.Deleted)
            return ChannelAccessDecision.Denied;

        if (space.SpaceType == SpaceType.Channel)
        {
            var channel = await channelAccess.CheckAsync(userId, space.Id, cancellationToken);
            return new(channel.CanRead, channel.CanSend && space.Status == SpaceStatus.Active);
        }

        if (space.SpaceType is not (SpaceType.Direct or SpaceType.Group))
            return ChannelAccessDecision.Denied;

        var member = await dbContext.SpaceMembers.AsNoTracking().AnyAsync(
            item => item.SpaceId == space.Id
                    && item.UserId == userId
                    && item.MembershipStatus == SpaceMembershipStatus.Active,
            cancellationToken);
        if (!member) return ChannelAccessDecision.Denied;

        var conversationExists = space.SpaceType == SpaceType.Direct
            ? await dbContext.DirectConversations.AsNoTracking().AnyAsync(item => item.SpaceId == space.Id, cancellationToken)
            : await dbContext.GroupConversations.AsNoTracking().AnyAsync(item => item.SpaceId == space.Id, cancellationToken);
        return conversationExists
            ? new ChannelAccessDecision(true, space.Status == SpaceStatus.Active)
            : ChannelAccessDecision.Denied;
    }
}
