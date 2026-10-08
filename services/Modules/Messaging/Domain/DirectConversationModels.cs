namespace SCDC.Modules.Messaging.Domain;

internal sealed class ChatSpace
{
    public Guid Id { get; set; }
    public short SpaceType { get; set; } = 1;
    public short Status { get; set; } = 1;
    public Guid? CreatedByUserId { get; set; }
    public long? LastMessageSequence { get; set; }
    public DateTimeOffset? LastActivityAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

internal sealed class DirectConversation
{
    public Guid SpaceId { get; set; }
    public short SpaceType { get; set; } = 1;
    public Guid UserLowId { get; set; }
    public Guid UserHighId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

internal sealed class SpaceMember
{
    public Guid SpaceId { get; set; }
    public Guid UserId { get; set; }
    public short MemberRole { get; set; } = 1;
    public short MembershipStatus { get; set; } = 1;
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LeftAt { get; set; }
}
