using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Messaging.Realtime;

internal sealed class SubscriptionReconciler(IServiceScopeFactory scopes, ConversationRegistry registry, IHubContext<ChatHub> hub, TimeProvider clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.WhenAll(registry.Snapshot().Select(entry => CheckAsync(entry, token)));
            await Task.Delay(1000, token);
        }
    }
    private async Task CheckAsync(ConversationRegistry.Connection entry, CancellationToken token)
    {
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token, entry.Aborted);
                deadline.CancelAfter(TimeSpan.FromSeconds(2));
                var entered = false;
                try
                {
                    await entry.Gate.WaitAsync(deadline.Token); entered = true;
                    if (entry.Closed || entry.Aborted.IsCancellationRequested) return;
                    if (entry.Actor.AccessExpiresAt <= clock.GetUtcNow())
                    {
                        // The transport credential expired, not necessarily the refresh session.
                        // Close it and let the SDK obtain a fresh credential for REST recovery.
                        entry.Spaces.Clear(); entry.Closed = true; entry.Abort(); return;
                    }
                    await using var scope = scopes.CreateAsyncScope();
                    var session = await scope.ServiceProvider.GetRequiredService<IAuthenticatedSessionReader>().ReadAsync(entry.Actor, deadline.Token);
                    if (!session.Allowed)
                    {
                        entry.Spaces.Clear(); entry.Closed = true;
                        try { await hub.Clients.Client(entry.Id).SendAsync("AccessRevoked", new { scope = session.Reason == "account_unavailable" ? "user" : "session", conversationId = (Guid?)null, reason = session.Reason }, deadline.Token); }
                        finally { entry.Abort(); }
                        return;
                    }
                    foreach (var space in entry.Spaces.ToArray())
                    {
                        await using var check = scopes.CreateAsyncScope();
                        var access = await check.ServiceProvider.GetRequiredService<ConversationAccess>().AuthorizeAsync(entry.Actor, space, deadline.Token);
                        if (access.IsSuccess) continue;
                        if (access.Error.Code == "AUTHORITY_UNAVAILABLE") throw new InvalidOperationException();
                        entry.Spaces.Remove(space);
                        await hub.Clients.Client(entry.Id).SendAsync("AccessRevoked", new { scope = "conversation", conversationId = space, reason = "access_removed" }, deadline.Token);
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                catch { if (entered) entry.Spaces.Clear(); entry.Closed = true; entry.Abort(); } // Cannot confirm authority: stop routing.
                finally { if (entered) entry.Gate.Release(); }
    }
}
