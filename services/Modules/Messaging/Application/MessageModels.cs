using SCDC.Contracts.Identity;

namespace SCDC.Modules.Messaging.Application;

public sealed record SendMessageCommand(
    Guid ActorUserId,
    Guid SpaceId,
    Guid ClientMessageId,
    short MessageType,
    string? Content);

public sealed record SendMessageResult(MessageDto Message, bool Created);

public sealed record GetMessagesQuery(
    Guid ActorUserId,
    Guid SpaceId,
    int Limit,
    string? BeforeSequence,
    string? AfterSequence,
    string? ThroughSequence);

public sealed record MessagePageDto(
    IReadOnlyList<MessageDto> Items,
    bool HasMore,
    string? NextBeforeSequence,
    string? NextAfterSequence,
    string HighWatermark);

public sealed record MessageDto(
    Guid Id,
    Guid SpaceId,
    Guid? ClientMessageId,
    string SequenceNo,
    short MessageType,
    UserSummary? Author,
    string? Content,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt,
    DateTimeOffset? DeletedAt,
    Guid? ReplyToMessageId,
    Guid? ThreadRootId,
    IReadOnlyList<AttachmentSummaryDto> Attachments,
    IReadOnlyList<ReactionSummaryDto> Reactions,
    bool IsPinned,
    int ThreadCount);

public sealed record AttachmentSummaryDto(
    Guid Id,
    string Name,
    string MimeType,
    string SizeBytes,
    int? Width,
    int? Height);

public sealed record ReactionSummaryDto(
    string ReactionKey,
    int Count,
    bool ReactedByMe);
