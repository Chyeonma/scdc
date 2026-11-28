using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Messaging.Application;

public sealed record MessageLikes(int Count, bool LikedByMe);
public sealed record GroupMessageLikeRequest(AuthenticatedSession Actor, Guid ConversationId, Guid MessageId);

public interface IGroupMessageLikes
{
    Task<Result<MessageLikes>> ReadAsync(GroupMessageLikeRequest request, CancellationToken token);
    Task<Result<MessageLikes>> SetAsync(GroupMessageLikeRequest request, bool liked, CancellationToken token);
}
