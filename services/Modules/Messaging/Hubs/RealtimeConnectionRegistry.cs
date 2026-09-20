using System.Collections.Concurrent;

namespace SCDC.Modules.Messaging.Hubs;

public sealed class RealtimeConnectionRegistry
{
    private readonly ConcurrentDictionary<string, ConnectionEntry> _connections = new(StringComparer.Ordinal);

    public void Register(Guid userId, Guid sessionId, string connectionId, Action abort)
    {
        _connections[connectionId] = new ConnectionEntry(userId, sessionId, connectionId, abort);
    }

    public void Remove(string connectionId) => _connections.TryRemove(connectionId, out _);

    public bool Subscribe(string connectionId, Guid spaceId) =>
        _connections.TryGetValue(connectionId, out var connection)
        && connection.SubscribedSpaces.TryAdd(spaceId, 0);

    public bool Unsubscribe(string connectionId, Guid spaceId) =>
        _connections.TryGetValue(connectionId, out var connection)
        && connection.SubscribedSpaces.TryRemove(spaceId, out _);

    internal IReadOnlyList<RealtimeConnection> GetSubscribers(Guid spaceId) => _connections.Values
        .Where(connection => connection.SubscribedSpaces.ContainsKey(spaceId))
        .Select(ToSnapshot)
        .ToArray();

    internal IReadOnlyList<RealtimeConnection> GetUserConnections(Guid userId) => _connections.Values
        .Where(connection => connection.UserId == userId)
        .Select(ToSnapshot)
        .ToArray();

    internal IReadOnlyList<RealtimeConnection> GetSessionConnections(Guid userId, Guid sessionId) => _connections.Values
        .Where(connection => connection.UserId == userId && connection.SessionId == sessionId)
        .Select(ToSnapshot)
        .ToArray();

    public bool IsSubscribed(string connectionId, Guid spaceId) =>
        _connections.TryGetValue(connectionId, out var connection)
        && connection.SubscribedSpaces.ContainsKey(spaceId);

    private static RealtimeConnection ToSnapshot(ConnectionEntry connection) => new(
        connection.UserId,
        connection.SessionId,
        connection.ConnectionId,
        connection.Abort);

    private sealed class ConnectionEntry(Guid userId, Guid sessionId, string connectionId, Action abort)
    {
        public Guid UserId { get; } = userId;
        public Guid SessionId { get; } = sessionId;
        public string ConnectionId { get; } = connectionId;
        public Action Abort { get; } = abort;
        public ConcurrentDictionary<Guid, byte> SubscribedSpaces { get; } = new();
    }
}

internal sealed record RealtimeConnection(Guid UserId, Guid SessionId, string ConnectionId, Action Abort);
