using System.Collections.Concurrent;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class MessageRateLimiter(TimeProvider timeProvider)
{
    private const int MaxMessagesPerWindow = 30;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<Guid, Queue<DateTimeOffset>> _requests = new();

    public bool TryAcquire(Guid userId)
    {
        var now = timeProvider.GetUtcNow();
        var requests = _requests.GetOrAdd(userId, _ => new Queue<DateTimeOffset>());
        lock (requests)
        {
            while (requests.TryPeek(out var oldest) && oldest <= now - Window)
            {
                requests.Dequeue();
            }

            if (requests.Count >= MaxMessagesPerWindow)
            {
                return false;
            }

            requests.Enqueue(now);
            return true;
        }
    }
}
