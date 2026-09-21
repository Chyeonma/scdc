namespace SCDC.Modules.Messaging.Application;

public interface IRealtimeMessagePublisher
{
    Task PublishMessageCreatedAsync(
        RealtimeMessageCreated notification,
        CancellationToken cancellationToken);
}

public sealed record RealtimeMessageCreated(
    Guid EventId,
    Guid SpaceId,
    Guid MessageId,
    string SequenceNo,
    int AggregateVersion,
    DateTimeOffset OccurredAt);
