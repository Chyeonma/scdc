namespace SCDC.Modules.Messaging.Domain;

internal sealed class DirectConversation
{
    public Guid SpaceId { get; set; }
    public Guid UserLowId { get; set; }
    public Guid UserHighId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
