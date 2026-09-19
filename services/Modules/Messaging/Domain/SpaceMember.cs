namespace SCDC.Modules.Messaging.Domain;

internal sealed class SpaceMember
{
    public Guid SpaceId { get; set; }
    public Guid UserId { get; set; }
    public SpaceMemberRole MemberRole { get; set; }
    public SpaceMembershipStatus MembershipStatus { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LeftAt { get; set; }
    public Guid? RemovedByUserId { get; set; }
}
