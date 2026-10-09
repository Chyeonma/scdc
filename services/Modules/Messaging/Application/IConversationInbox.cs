using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Messaging.Application;

public sealed record InboxRequest(Guid ActorId, Guid SessionId, Guid SecurityStamp, int Limit = 20, string? Cursor = null);
public sealed record DirectConversationPage(IReadOnlyList<DirectConversationResponse> Items, string? NextCursor);
public interface IConversationInbox
{
    Task<Result<DirectConversationPage>> ListAsync(InboxRequest request, CancellationToken cancellationToken);
}
