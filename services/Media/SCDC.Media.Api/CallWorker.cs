using SCDC.Media.Application;
using SCDC.Media.Domain;

namespace SCDC.Media.Api;

public sealed class CallWorker(IServiceScopeFactory scopes, TimeProvider clock, ILogger<CallWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope(); var services = scope.ServiceProvider;
                var store = services.GetRequiredService<ICallStore>(); var calls = services.GetRequiredService<CallService>();
                var registry = services.GetRequiredService<MediaRegistry>(); var proxy = services.GetRequiredService<LiveKitProxy>();
                registry.Prune(); proxy.Prune();
                foreach (var current in await store.PendingAsync(stoppingToken))
                {
                    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken); deadline.CancelAfter(TimeSpan.FromSeconds(4));
                    var call = current;
                    if (call.Status == "ringing" && clock.GetUtcNow() >= call.RingExpiresAt)
                        call = await store.EndAsync(call.Id, "no_answer", deadline.Token);
                    else if (call.Status != "ended")
                    {
                        try
                        {
                            await calls.Check(calls.Caller(call), deadline.Token);
                            if (call.Status == "active")
                            {
                                await calls.CheckCall(call, deadline.Token);
                                foreach (var (user, session, instance) in new[] {
                                    (call.CallerId, call.CallerSessionId, call.CallerInstanceId),
                                    (call.PeerId, call.PeerSessionId!.Value, call.PeerInstanceId!.Value) })
                                {
                                    var owner = registry.Owner(user, session, instance);
                                    var last = owner?.DisconnectedAt ?? owner?.LastSeen ?? call.UpdatedAt;
                                    if (last < clock.GetUtcNow().AddSeconds(-30) || !proxy.Connected(call.Id, instance) && proxy.LastSeen(call.Id, instance, call.UpdatedAt) < clock.GetUtcNow().AddSeconds(-30))
                                        throw new MediaProblem(409, "Media.ConnectionLost", "Reconnect deadline expired.");
                                }
                            }
                        }
                        catch (Exception e) when (e is MediaProblem or OperationCanceledException or HttpRequestException)
                        { call = await store.EndAsync(call.Id, e is MediaProblem { Status: 401 or 403 } ? "session_revoked" : "connection_lost", stoppingToken); }
                    }
                    if (call.Status == "ended" && !call.ProviderClosed)
                    {
                        proxy.Close(call.Id);
                        var gate = services.GetRequiredService<ProviderGate>();
                        await gate.Lock.WaitAsync(deadline.Token);
                        try {
                            await services.GetRequiredService<IMediaProvider>().DeleteAsync(call, deadline.Token);
                            await store.ClosedAsync(call.Id, deadline.Token);
                        } finally { gate.Lock.Release(); }
                    }
                    if (call.Version != current.Version) await calls.Notify(call, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch { log.LogWarning("Media reconciliation deferred: {Code}", "MEDIA_RECONCILIATION_UNAVAILABLE"); }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
