namespace SCDC.Modules.Messaging.Domain;

internal sealed class MessageEdit
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public int Version { get; set; }
    public string? PreviousContent { get; set; }
    public Guid EditedByUserId { get; set; }
    public DateTimeOffset EditedAt { get; set; }
}
