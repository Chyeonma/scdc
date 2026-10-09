using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Messaging.Application;

public sealed record SendTextMessage(Guid ActorId, Guid SessionId, Guid SecurityStamp,
    Guid ConversationId, Guid ClientMessageId, string? Content);
public sealed record MessageResponse(Guid Id, Guid ConversationId, UserSummary Author, Guid ClientMessageId,
    string Sequence, string Version, string? Content, DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt, DateTimeOffset? DeletedAt);
public interface ITextMessageSender
{
    Task<Result<MessageResponse>> SendAsync(SendTextMessage command, CancellationToken cancellationToken);
}
