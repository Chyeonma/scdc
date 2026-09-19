namespace SCDC.Modules.Messaging.Domain;

internal sealed class ChatSpace
{
    public Guid Id { get; set; }
    public SpaceType SpaceType { get; set; }
    public SpaceStatus Status { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? LastMessageId { get; set; }
    public long? LastMessageSequence { get; set; }
    public DateTimeOffset? LastActivityAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public int Version { get; set; }
}
