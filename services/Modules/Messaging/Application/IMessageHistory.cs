using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Messaging.Application;

public sealed record HistoryRequest(Guid ActorId, Guid SessionId, Guid SecurityStamp, Guid ConversationId,
    int Limit, string? Before, string? After, string? Through);
public sealed record MessagePage(IReadOnlyList<MessageResponse> Items, string? NextCursor, bool HasMore,
    string ThroughSequence, string? ResumeCursor);
public interface IMessageHistory
{
    Task<Result<MessagePage>> ReadAsync(HistoryRequest request, CancellationToken cancellationToken);
}
