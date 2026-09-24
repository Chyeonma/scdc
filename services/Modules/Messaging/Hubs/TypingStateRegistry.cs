namespace SCDC.Modules.Messaging.Hubs;

public sealed class TypingStateRegistry(TimeProvider timeProvider)
{
    private readonly Dictionary<(string ConnectionId, Guid SpaceId), Entry> _entries = new();
    private readonly object _gate = new();
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan MinimumBroadcastInterval = TimeSpan.FromSeconds(2);

    public bool Start(string connectionId, Guid userId, Guid spaceId, out DateTimeOffset expiresAt)
    {
        lock (_gate)
        {
            var now = timeProvider.GetUtcNow();
            RemoveExpired(now);
            var key = (connectionId, spaceId);
            if (_entries.TryGetValue(key, out var previous)
                && now - previous.LastBroadcastAt < MinimumBroadcastInterval)
            {
                expiresAt = previous.ExpiresAt;
                return false;
            }

            expiresAt = now + Lifetime;
            _entries[key] = new Entry(userId, expiresAt, now);
            return true;
        }
    }

    public bool Stop(string connectionId, Guid userId, Guid spaceId)
    {
        lock (_gate)
        {
            RemoveExpired(timeProvider.GetUtcNow());
            if (!_entries.Remove((connectionId, spaceId))) return false;
            return !_entries.Any(item => item.Key.SpaceId == spaceId && item.Value.UserId == userId);
        }
    }

    public void RemoveConnection(string connectionId)
    {
        lock (_gate)
        {
            foreach (var key in _entries.Keys.Where(key => key.ConnectionId == connectionId).ToArray())
                _entries.Remove(key);
        }
    }

    private void RemoveExpired(DateTimeOffset now)
    {
        foreach (var key in _entries.Where(item => item.Value.ExpiresAt <= now).Select(item => item.Key).ToArray())
            _entries.Remove(key);
    }

    private sealed record Entry(Guid UserId, DateTimeOffset ExpiresAt, DateTimeOffset LastBroadcastAt);
}
