namespace SCDC.Modules.Messaging.Domain;

internal sealed class UserBlock
{
    public Guid BlockerUserId { get; set; }
    public Guid BlockedUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
