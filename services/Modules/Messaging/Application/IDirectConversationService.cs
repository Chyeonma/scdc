using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Messaging.Application;

public sealed record OpenDirectConversation(Guid ActorId, Guid SessionId, Guid SecurityStamp, Guid PeerUserId);
public sealed record DirectConversationResponse(Guid Id, IReadOnlyList<UserSummary> Participants,
    DateTimeOffset CreatedAt, string LastSequence, DateTimeOffset? LastActivityAt);

public interface IDirectConversationService
{
    Task<Result<DirectConversationResponse>> OpenAsync(OpenDirectConversation command, CancellationToken cancellationToken);
}
