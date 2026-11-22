using Microsoft.AspNetCore.SignalR;
using SCDC.Media.Application;
using SCDC.Media.Domain;

namespace SCDC.Media.Api;

public sealed class CallService(ICallStore store, MediaRegistry registry, IIdentityAuthority authority,
    IMediaProvider provider, IHubContext<MediaHub> hub, TimeProvider clock, ProviderGate gate)
{
    public async Task Check(MediaActor actor, CancellationToken ct)
    {
        if (actor.ExpiresAt <= clock.GetUtcNow() || (await authority.CheckAsync(actor.AccessToken, ct)).Id != actor.UserId)
            throw new MediaProblem(401, "Media.SessionInvalid", "The authenticated session is unavailable.");
    }
    public Task Notify(CallRecord call, CancellationToken ct) => hub.Clients.Users(call.CallerId.ToString(), call.PeerId.ToString())
        .SendAsync(call.Status == "ringing" ? "IncomingCall" : "CallChanged", new { callId = call.Id, call.Status, version = call.Version.ToString() }, ct);
    public async Task<CallRecord> Create(MediaActor actor, Guid peerId, Guid operation, CancellationToken ct)
    {
        await Check(actor, ct); var peer = registry.Peer(peerId); await Check(peer, ct);
        var call = await store.CreateAsync(actor, peer, operation, ct); await Notify(call, ct); return call;
    }
    public async Task<CallRecord> Act(Guid id, MediaActor actor, string action, string version, Guid operation, CancellationToken ct)
    {
        await Check(actor, ct);
        await gate.Lock.WaitAsync(ct);
        try {
        var existing = await store.GetAsync(id, ct);
        if (!existing.Contains(actor.UserId)) throw new MediaProblem(404, "Media.NotFound", "Call unavailable.");
        if (action == "accept" && existing.Status == "ringing") await Check(Caller(existing), ct);
        var call = await store.ActAsync(id, actor, action, version, operation, ct);
        if (call.Status == "active" && action == "accept")
        {
            try { await provider.CreateAsync(call, ct); }
            catch { await store.EndAsync(id, "provider_unavailable", CancellationToken.None); throw; }
        }
        await Notify(call, ct); return call;
        } finally { gate.Lock.Release(); }
    }
    public MediaActor Caller(CallRecord call) => registry.Owner(call.CallerId, call.CallerSessionId, call.CallerInstanceId)?.Actor
        ?? throw new MediaProblem(409, "Media.PeerUnavailable", "Caller is unavailable.");
    public async Task CheckCall(CallRecord call, CancellationToken ct)
    {
        if (call.Status != "active") throw new MediaProblem(409, "Media.Expired", "Call is not active.");
        await Check(Caller(call), ct);
        var peer = registry.Owner(call.PeerId, call.PeerSessionId!.Value, call.PeerInstanceId!.Value)?.Actor
            ?? throw new MediaProblem(409, "Media.PeerUnavailable", "Recipient is unavailable.");
        await Check(peer, ct);
    }
}
