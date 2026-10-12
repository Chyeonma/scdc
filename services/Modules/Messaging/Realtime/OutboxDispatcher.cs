using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Messaging.Realtime;

public sealed class OutboxDispatcher(IServiceScopeFactory scopes, ConversationRegistry registry,
    IHubContext<ChatHub> hub, IConfiguration configuration, IHostEnvironment environment, ILogger<OutboxDispatcher> logger, TimeProvider clock) : BackgroundService
{
    private string? ControlDirectory => environment.IsDevelopment() ? configuration["Modules:Messaging:Dispatcher:DevelopmentControlDirectory"] : null;
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        if (!configuration.GetValue("Modules:Messaging:Dispatcher:Enabled", true)) return;
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (ControlDirectory is { } directory && File.Exists(Path.Combine(directory, "pause")))
                {
                    var nonce = await File.ReadAllTextAsync(Path.Combine(directory, "pause"), token);
                    var temporary = Path.Combine(directory, "pause-ack.tmp");
                    await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(new { nonce, acknowledgedAt = DateTimeOffset.UtcNow }), token);
                    File.Move(temporary, Path.Combine(directory, "pause-ack.json"), true);
                    await Task.Delay(100, token);
                }
                else if (!await DispatchOnceAsync(token)) await Task.Delay(100, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch
            {
                // Provider/transport exceptions can include private data. Record only a fixed code.
                logger.LogWarning("Outbox loop deferred {ErrorCode}", "DISPATCH_UNAVAILABLE");
                await Task.Delay(1000, token);
            }
        }
    }
    public async Task<bool> DispatchOnceAsync(CancellationToken token = default, Guid? conversationId = null)
    {
        OutboxClaim? claim;
        await using (var scope = scopes.CreateAsyncScope())
            claim = await scope.ServiceProvider.GetRequiredService<OutboxStore>().ClaimAsync(
                Math.Clamp(configuration.GetValue("Modules:Messaging:Dispatcher:LeaseSeconds", 15), 1, 60), conversationId, token);
        if (claim is null) return false;
        try
        {
            // Optional Development-only process barrier, outside the API/product flow. Used to
            // kill a real worker after its committed claim and prove expired-lease recovery.
            if (ControlDirectory is { } directory && File.Exists(Path.Combine(directory, "hold-" + claim.Id)))
            {
                Directory.CreateDirectory(directory);
                await File.WriteAllTextAsync(Path.Combine(directory, "claimed-" + claim.Id + ".json"),
                    JsonSerializer.Serialize(new { eventId = claim.Id, claim.SpaceId, claim.MessageId, claim.LeaseToken, claim.LeaseExpiresAt }), token);
                while (File.Exists(Path.Combine(directory, "hold-" + claim.Id))) await Task.Delay(50, token);
            }
            foreach (var entry in registry.Snapshot())
            {
                await entry.Gate.WaitAsync(token);
                try
                {
                    if (entry.Closed || entry.Aborted.IsCancellationRequested || !entry.Spaces.Contains(claim.SpaceId)) continue;
                    if (entry.Actor.AccessExpiresAt <= clock.GetUtcNow())
                    {
                        entry.Spaces.Clear(); entry.Closed = true; entry.Abort(); continue;
                    }
                    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token, entry.Aborted);
                    deadline.CancelAfter(TimeSpan.FromSeconds(2));
                    await using var scope = scopes.CreateAsyncScope();
                    var access = await scope.ServiceProvider.GetRequiredService<ConversationAccess>().DeliverAsync(
                        entry.Actor, claim.SpaceId, claim.MessageId, message => hub.Clients.Client(entry.Id).SendAsync("MessageChanged",
                            new { eventId = claim.Id, conversationId = claim.SpaceId, message }, deadline.Token), deadline.Token);
                    if (access.IsFailure)
                    {
                        if (access.Error.Code == "AUTHORITY_UNAVAILABLE") throw new InvalidOperationException("DISPATCH_UNAVAILABLE");
                        if (access.Error.Code == "Common.Unauthorized")
                        {
                            var session = await scope.ServiceProvider.GetRequiredService<IAuthenticatedSessionReader>().ReadAsync(entry.Actor, deadline.Token);
                            entry.Spaces.Clear(); entry.Closed = true;
                            try { await hub.Clients.Client(entry.Id).SendAsync("AccessRevoked", new { scope = session.Reason == "account_unavailable" ? "user" : "session", conversationId = (Guid?)null, reason = session.Reason ?? "session_revoked" }, deadline.Token); }
                            finally { entry.Abort(); }
                            continue;
                        }
                        entry.Spaces.Remove(claim.SpaceId);
                        await hub.Clients.Client(entry.Id).SendAsync("AccessRevoked", new { scope = "conversation", conversationId = claim.SpaceId, reason = "access_removed" }, deadline.Token);
                    }
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested && entry.Aborted.IsCancellationRequested) { }
                finally { entry.Gate.Release(); }
            }
            await using var finish = scopes.CreateAsyncScope();
            await finish.ServiceProvider.GetRequiredService<OutboxStore>().CompleteAsync(claim, null, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; } // Leave lease for crash/restart recovery.
        catch
        {
            logger.LogWarning("Outbox delivery deferred {EventId} {ErrorCode}", claim.Id, "DISPATCH_UNAVAILABLE");
            await using var retry = scopes.CreateAsyncScope();
            await retry.ServiceProvider.GetRequiredService<OutboxStore>().CompleteAsync(claim, "DISPATCH_UNAVAILABLE", token);
        }
        return true;
    }
}
