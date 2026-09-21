namespace SCDC.Modules.Messaging.Domain;

internal sealed class SpaceUserState
{
    public Guid SpaceId { get; set; }
    public Guid UserId { get; set; }
    public long? LastReadSequence { get; set; }
    public DateTimeOffset? LastReadAt { get; set; }
    public NotificationLevel NotificationLevel { get; set; }
    public DateTimeOffset? MutedUntil { get; set; }
    public bool IsHidden { get; set; }
    public bool IsPinned { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
