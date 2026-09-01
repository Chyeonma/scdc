namespace SCDC.Modules.Identity.Domain;

internal sealed class OutboxEvent
{
    public Guid Id { get; set; }
    public required string EventType { get; set; }
    public required string AggregateType { get; set; }
    public Guid AggregateId { get; set; }
    public int? AggregateVersion { get; set; }
    public Guid? SpaceId { get; set; }
    public required string Payload { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
