using System.Collections.Concurrent;

namespace ChatService.Common.Realtime;

public sealed class ChatSubscriptionRegistry
{
    private readonly ConcurrentDictionary<(Guid UserId, Guid ChannelId), ConcurrentDictionary<string, byte>> _subscriptions = new();

    public void Add(Guid userId, Guid channelId, string connectionId)
    {
        var connections = _subscriptions.GetOrAdd(
            (userId, channelId),
            static _ => new ConcurrentDictionary<string, byte>());
        connections.TryAdd(connectionId, 0);
    }

    public void Remove(Guid userId, Guid channelId, string connectionId)
    {
        var key = (userId, channelId);
        if (!_subscriptions.TryGetValue(key, out var connections))
        {
            return;
        }

        connections.TryRemove(connectionId, out _);
        if (connections.IsEmpty)
        {
            _subscriptions.TryRemove(key, out _);
        }
    }

    public IReadOnlyList<string> RemoveUserFromChannel(Guid userId, Guid channelId)
    {
        return _subscriptions.TryRemove((userId, channelId), out var connections)
            ? connections.Keys.ToArray()
            : [];
    }

    public void RemoveConnection(string connectionId)
    {
        foreach (var entry in _subscriptions)
        {
            entry.Value.TryRemove(connectionId, out _);
            if (entry.Value.IsEmpty)
            {
                _subscriptions.TryRemove(entry.Key, out _);
            }
        }
    }
}

public static class ChatGroups
{
    public static string Channel(Guid channelId) => $"channel:{channelId:N}";
}
