using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Domain;
using System.Text.Json.Serialization;

namespace SCDC.Modules.Messaging.Application;

public sealed record SendTextMessage(Guid ActorId, Guid SessionId, Guid SecurityStamp,
    Guid ConversationId, Guid ClientMessageId, string? Content, DateTimeOffset AccessExpiresAt = default,
    ConversationKind Kind = ConversationKind.Direct, Guid? ReplyToMessageId = null);
public sealed record MessageReply(Guid Id, Guid AuthorUserId, string? Content, bool IsDeleted);
public sealed record MessageResponse(Guid Id, Guid ConversationId, UserSummary Author, Guid ClientMessageId,
    string Sequence, string Version, string? Content, DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt, DateTimeOffset? DeletedAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] MessageLikes? Likes = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] MessageReply? Reply = null);
public interface ITextMessageSender
{
    Task<Result<MessageResponse>> SendAsync(SendTextMessage command, CancellationToken cancellationToken);
}
