namespace SCDC.Modules.Messaging.Domain;

internal sealed class TextMessage
{
    public Guid Id { get; set; }
    public Guid SpaceId { get; set; }
    public Guid AuthorUserId { get; set; }
    public Guid ClientMessageId { get; set; }
    public short MessageType { get; set; } = 1;
    public long ConversationSequence { get; set; }
    public int Version { get; set; } = 1;
    public string? Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
internal sealed class SendOperation
{
    public Guid SpaceId { get; set; }
    public Guid AuthorUserId { get; set; }
    public Guid ClientMessageId { get; set; }
    public Guid MessageId { get; set; }
    public int? FingerprintVersion { get; set; }
    public string? KeyId { get; set; }
    public byte[]? Fingerprint { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
internal sealed class MessageOutbox
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = "Messaging.MessageChanged";
    public string AggregateType { get; set; } = "Message";
    public Guid AggregateId { get; set; }
    public int AggregateVersion { get; set; } = 1;
    public Guid SpaceId { get; set; }
    public string Payload { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
}
