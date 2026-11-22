using System.Collections.Concurrent;
using System.Net.WebSockets;
using SCDC.Media.Application;
using SCDC.Media.Domain;

namespace SCDC.Media.Api;

// The SFU signaling port is internal. Every admission must still own an active call.
public sealed class LiveKitProxy(IServiceScopeFactory scopes, IHttpClientFactory clients, IConfiguration config, TimeProvider clock)
{
    private sealed record Lease(Guid Call, Guid Instance, CancellationTokenSource Stop);
    private readonly ConcurrentDictionary<string, Lease> leases = new();
    private readonly ConcurrentDictionary<(Guid, Guid), DateTimeOffset> lastSeen = new();
    public bool Connected(Guid call, Guid instance) => leases.Values.Any(l => l.Call == call && l.Instance == instance);
    public DateTimeOffset LastSeen(Guid call, Guid instance, DateTimeOffset fallback) => lastSeen.GetValueOrDefault((call, instance), fallback);
    public void Close(Guid call) { foreach (var lease in leases.Values.Where(l => l.Call == call)) lease.Stop.Cancel(); }
    public void Prune()
    {
        foreach (var item in lastSeen.Where(p => p.Value < clock.GetUtcNow().AddMinutes(-5))) lastSeen.TryRemove(item.Key, out _);
    }
    public async Task Handle(HttpContext context)
    {
        var path = context.Request.Path.Value!["/livekit/".Length..];
        if (context.Request.Method != "GET" || path is not ("rtc" or "rtc/validate" or "rtc/v1" or "rtc/v1/validate"))
            throw new MediaProblem(404, "Media.NotFound", "Endpoint unavailable.");
        using var scope = scopes.CreateScope(); var services = scope.ServiceProvider;
        var provider = services.GetRequiredService<IMediaProvider>();
        var token = context.Request.Query["access_token"].ToString();
        if (string.IsNullOrEmpty(token) && context.Request.Headers.Authorization.ToString().StartsWith("Bearer ")) token = context.Request.Headers.Authorization.ToString()[7..];
        if (!provider.Validate(token, out var id, out var user, out var session, out var instance))
            throw new MediaProblem(401, "Media.InvalidGrant", "Media grant expired or invalid.");
        var actor = services.GetRequiredService<MediaRegistry>().Require(user, session, instance);
        var call = await services.GetRequiredService<ICallStore>().GetAsync(id, context.RequestAborted);
        if (!call.Owns(actor)) throw new MediaProblem(403, "Media.InvalidGrant", "This tab does not own the call.");
        await services.GetRequiredService<CallService>().CheckCall(call, context.RequestAborted);
        var upstream = new Uri(new Uri(config["LiveKit:BaseUrl"]!), path + context.Request.QueryString);
        context.Response.Headers.CacheControl = "no-store";
        if (!context.WebSockets.IsWebSocketRequest)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, upstream);
            request.Headers.Authorization = new("Bearer", token);
            using var response = await clients.CreateClient("signaling").SendAsync(request, context.RequestAborted);
            context.Response.StatusCode = (int)response.StatusCode;
            context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "text/plain";
            await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted); return;
        }
        var identity = Infrastructure.LiveKitProvider.Identity(id, user, instance);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        var lease = new Lease(id, instance, stop);
        if (!leases.TryAdd(identity, lease)) throw new MediaProblem(409, "Media.ConnectionInUse", "This tab is already connected.");
        try
        {
            using var remote = new ClientWebSocket(); remote.Options.KeepAliveInterval = TimeSpan.FromSeconds(5);
            var target = new UriBuilder(upstream) { Scheme = upstream.Scheme == "https" ? "wss" : "ws" };
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop.Token))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(5)); await remote.ConnectAsync(target.Uri, timeout.Token);
            }
            // Recheck after upstream admission in case an end raced with the connection handshake.
            await services.GetRequiredService<CallService>().CheckCall(await services.GetRequiredService<ICallStore>().GetAsync(id, stop.Token), stop.Token);
            using var browser = await context.WebSockets.AcceptWebSocketAsync();
            var outbound = Pump(browser, remote, stop.Token); var inbound = Pump(remote, browser, stop.Token);
            await Task.WhenAny(outbound, inbound); await stop.CancelAsync();
            try { await Task.WhenAll(outbound, inbound); } catch (Exception e) when (e is OperationCanceledException or WebSocketException) { }
            browser.Abort(); remote.Abort();
        }
        catch (Exception e) when (e is OperationCanceledException or WebSocketException)
        { if (!context.Response.HasStarted) context.Response.StatusCode = 503; }
        finally { leases.TryRemove(identity, out _); lastSeen[(id, instance)] = clock.GetUtcNow(); }
    }
    private static async Task Pump(WebSocket source, WebSocket target, CancellationToken ct)
    {
        var buffer = new byte[65536];
        while (!ct.IsCancellationRequested)
        {
            var result = await source.ReceiveAsync(buffer.AsMemory(), ct);
            if (result.MessageType == WebSocketMessageType.Close) break;
            await target.SendAsync(buffer.AsMemory(0, result.Count), result.MessageType, result.EndOfMessage, ct);
        }
    }
}
