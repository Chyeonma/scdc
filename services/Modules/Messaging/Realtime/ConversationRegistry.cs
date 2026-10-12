using System.Collections.Concurrent;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Messaging.Realtime;

// MVP single host. A connection gate serializes subscribe/unsubscribe, reconciliation and delivery.
// SignalR groups are not an authorization source.
public sealed class ConversationRegistry
{
    public sealed class Connection(string id, AuthenticatedSession actor, CancellationToken aborted, Action abort)
    {
        public string Id { get; } = id;
        public AuthenticatedSession Actor { get; } = actor;
        public CancellationToken Aborted { get; } = aborted;
        public Action Abort { get; } = abort;
        public SemaphoreSlim Gate { get; } = new(1);
        public HashSet<Guid> Spaces { get; } = [];
        public bool Closed { get; set; }
    }
    private readonly ConcurrentDictionary<string, Connection> connections = new();
    public Connection? Find(string id) => connections.GetValueOrDefault(id);
    public Connection[] Snapshot() => connections.Values.ToArray();
    public void Add(Connection entry) => connections[entry.Id] = entry;
    public async Task RemoveAsync(string id)
    {
        if (!connections.TryRemove(id, out var entry)) return;
        await entry.Gate.WaitAsync();
        try { entry.Closed = true; entry.Spaces.Clear(); }
        finally { entry.Gate.Release(); }
    }
}
