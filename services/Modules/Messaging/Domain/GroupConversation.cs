namespace SCDC.Modules.Messaging.Domain;

internal sealed class GroupConversation
{
    public Guid SpaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarObjectKey { get; set; }
    public Guid OwnerUserId { get; set; }
    public int? MaxMembers { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
