using System.Collections.Concurrent;
using SCDC.Media.Domain;

namespace SCDC.Media.Api;

public sealed class MediaRegistry(TimeProvider clock)
{
    public sealed record Client(string ConnectionId, MediaActor Actor, DateTimeOffset LastSeen, DateTimeOffset? DisconnectedAt);
    private readonly ConcurrentDictionary<Guid, Client> clients = new();
    public void Prune()
    {
        foreach (var item in clients.Where(p => (p.Value.DisconnectedAt ?? p.Value.LastSeen) < clock.GetUtcNow().AddMinutes(-2)))
            clients.TryRemove(new KeyValuePair<Guid, Client>(item.Key, item.Value));
    }
    public void Register(string connection, MediaActor actor)
    {
        clients.AddOrUpdate(actor.InstanceId, _ => new(connection, actor, clock.GetUtcNow(), null), (_, previous) =>
        {
            if (previous.Actor.UserId != actor.UserId || previous.Actor.SessionId != actor.SessionId
                || previous.ConnectionId != connection && previous.DisconnectedAt is null)
                throw new MediaProblem(409, "Media.ConnectionInUse", "This instance belongs to another connection.");
            return new(connection, actor, clock.GetUtcNow(), null);
        });
    }
    public void Disconnected(string connection)
    {
        foreach (var (key, value) in clients)
            if (value.ConnectionId == connection) clients.TryUpdate(key, value with { DisconnectedAt = clock.GetUtcNow() }, value);
    }
    public Client? Owner(Guid user, Guid session, Guid instance) => clients.TryGetValue(instance, out var client)
        && client.Actor.UserId == user && client.Actor.SessionId == session ? client : null;
    public bool Ready(Client client) => client.DisconnectedAt is null && client.LastSeen > clock.GetUtcNow().AddSeconds(-15) && client.Actor.ExpiresAt > clock.GetUtcNow();
    public MediaActor Require(Guid user, Guid session, Guid instance)
    {
        CallRules.Uuid4(instance);
        var client = Owner(user, session, instance);
        return client is not null && Ready(client) ? client.Actor : throw new MediaProblem(409, "Media.ClientUnavailable", "Reconnect call notifications in this tab.");
    }
    public MediaActor Peer(Guid user) => clients.Values.FirstOrDefault(c => c.Actor.UserId == user && Ready(c))?.Actor
        ?? throw new MediaProblem(409, "Media.PeerUnavailable", "The recipient is not available for calls.");
}
