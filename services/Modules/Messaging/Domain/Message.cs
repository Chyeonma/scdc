namespace SCDC.Modules.Messaging.Domain;

internal sealed class Message
{
    public Guid Id { get; set; }
    public long SequenceNo { get; set; }
    public Guid SpaceId { get; set; }
    public Guid? AuthorUserId { get; set; }
    public Guid? ClientMessageId { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public Guid? ThreadRootId { get; set; }
    public MessageType MessageType { get; set; }
    public string? Content { get; set; }
    public string? IdempotencyPayloadHash { get; set; }
    public int Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
