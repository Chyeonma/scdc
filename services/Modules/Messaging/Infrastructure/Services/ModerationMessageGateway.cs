using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class ModerationMessageGateway(MessagingDbContext db, SpaceMessageAccess access,
    MessageService messages, IPlatformReviewAccess platform) : IModerationMessageGateway
{
    public async Task<ModerationMessageEvidence?> GetReportableAsync(Guid userId, Guid spaceId,
        Guid messageId, CancellationToken ct)
    {
        var space = await db.Spaces.AsNoTracking().SingleOrDefaultAsync(x => x.Id == spaceId, ct);
        if (space is null || !(await access.CheckAsync(userId, space, ct)).CanRead) return null;
        var message = await db.Messages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == messageId && x.SpaceId == spaceId, ct);
        return message is null || message.DeletedAt is not null || message.MessageType == MessageType.System
            ? null : new(message.Id, spaceId, message.AuthorUserId, (short)message.MessageType,
                message.Content, message.CreatedAt, message.Version);
    }

    public async Task<ModerationReviewAccess?> GetReviewAccessAsync(Guid userId, Guid spaceId, CancellationToken ct)
    {
        var space = await db.Spaces.AsNoTracking().SingleOrDefaultAsync(x => x.Id == spaceId, ct);
        if (space is null || space.Status == SpaceStatus.Deleted) return null;
        if (await platform.IsReviewerAsync(userId, ct)) return new(spaceId, null);
        var decision = await access.CheckAsync(userId, space, ct);
        if (!decision.CanRead) return null;
        if (space.SpaceType == SpaceType.Channel && decision.CanDeleteOthers)
            return new(spaceId, decision.ServerId);
        if (space.SpaceType == SpaceType.Group && await db.SpaceMembers.AsNoTracking().AnyAsync(
                x => x.SpaceId == spaceId && x.UserId == userId
                     && x.MembershipStatus == SpaceMembershipStatus.Active && x.MemberRole != SpaceMemberRole.Member, ct))
            return new(spaceId, null);
        return null;
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid spaceId, Guid messageId, CancellationToken ct)
    {
        if (await GetReviewAccessAsync(userId, spaceId, ct) is null) return false;
        var message = await db.Messages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == messageId && x.SpaceId == spaceId, ct);
        if (message is null) return false;
        var command = new DeleteMessageCommand(userId, spaceId, messageId, message.Version);
        var result = await (await platform.IsReviewerAsync(userId, ct)
            ? messages.DeleteForPlatformReviewAsync(command, ct)
            : messages.DeleteAsync(command, ct));
        return result.IsSuccess;
    }
}
