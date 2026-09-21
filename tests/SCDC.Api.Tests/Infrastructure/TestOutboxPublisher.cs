using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Tests.Infrastructure;

public sealed class TestOutboxPublisher : IRealtimeMessagePublisher
{
    private readonly object _gate = new();
    private readonly List<Guid> _eventIds = [];

    public Guid? CancelAfterEventId { get; set; }

    public CancellationTokenSource? CancellationSource { get; set; }

    public IReadOnlyList<Guid> EventIds
    {
        get
        {
            lock (_gate)
            {
                return _eventIds.ToArray();
            }
        }
    }

    public Task PublishMessageCreatedAsync(
        RealtimeMessageCreated notification,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _eventIds.Add(notification.EventId);
        }

        if (CancelAfterEventId == notification.EventId)
        {
            CancellationSource?.Cancel();
        }

        return Task.CompletedTask;
    }

    public void Reset()
    {
        lock (_gate)
        {
            _eventIds.Clear();
        }

        CancelAfterEventId = null;
        CancellationSource = null;
    }
}
