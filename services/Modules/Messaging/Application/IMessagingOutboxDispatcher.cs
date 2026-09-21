namespace SCDC.Modules.Messaging.Application;

public interface IMessagingOutboxDispatcher
{
    Task<int> DispatchDueAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<MessagingOutboxFailure>> ListFailuresAsync(
        int limit,
        CancellationToken cancellationToken);

    Task<bool> ReplayAsync(Guid eventId, CancellationToken cancellationToken);
}

public sealed record MessagingOutboxFailure(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    Guid? SpaceId,
    int AttemptCount,
    DateTimeOffset AvailableAt,
    DateTimeOffset OccurredAt,
    string? LastError);
