namespace SCDC.Modules.Messaging.Domain;

internal sealed class MessageMention
{
    public Guid MessageId { get; set; }
    public Guid MentionedUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
