namespace SCDC.Modules.Messaging.Domain;

public enum ConversationKind : short
{
    Direct = 1,
    Group = 2
}

internal sealed class GroupConversation
{
    public const int MaxMembers = 10;

    public Guid SpaceId { get; set; }
    public string Name { get; set; } = "";
    public Guid OwnerUserId { get; set; }
    public int? MaxMemberCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
