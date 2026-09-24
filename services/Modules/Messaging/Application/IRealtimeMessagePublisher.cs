namespace SCDC.Modules.Messaging.Application;

public interface IRealtimeMessagePublisher
{
    Task PublishMessageCreatedAsync(
        RealtimeMessageCreated notification,
        CancellationToken cancellationToken);

    Task PublishMessageChangedAsync(RealtimeMessageChanged notification, CancellationToken cancellationToken);
}

public sealed record RealtimeMessageCreated(
    Guid EventId,
    Guid SpaceId,
    Guid MessageId,
    string SequenceNo,
    int AggregateVersion,
    DateTimeOffset OccurredAt);

public sealed record RealtimeMessageChanged(
    Guid EventId, Guid SpaceId, Guid MessageId, string SequenceNo,
    int AggregateVersion, DateTimeOffset OccurredAt, DateTimeOffset? DeletedAt);
