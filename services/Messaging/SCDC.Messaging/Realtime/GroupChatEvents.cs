using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Domain;

namespace SCDC.Modules.Messaging.Realtime;

public sealed class GroupChatEvents(ConversationRegistry registry, IServiceScopeFactory scopes,
    IHubContext<ChatHub> hub, ILogger<GroupChatEvents> logger)
{
    internal Task TypingAsync(Guid conversationId, Guid userId, CancellationToken token) =>
        PublishAsync(conversationId, "GroupTyping", new { conversationId, userId }, userId, token);

    public Task LikesChangedAsync(Guid conversationId, Guid messageId, CancellationToken token) =>
        PublishAsync(conversationId, "GroupLikesChanged", new { conversationId, messageId }, null, token);

    private async Task PublishAsync(Guid conversationId, string method, object payload, Guid? exceptUser, CancellationToken token)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromSeconds(2));
        foreach (var entry in registry.Snapshot())
        {
            if (budget.IsCancellationRequested) return;
            if (entry.Actor.UserId == exceptUser || entry.Aborted.IsCancellationRequested) continue;
            try
            {
                await entry.Gate.WaitAsync(budget.Token);
                try
                {
                    if (entry.Closed || !entry.Spaces.Contains(conversationId)) continue;
                    using var recipient = CancellationTokenSource.CreateLinkedTokenSource(budget.Token, entry.Aborted);
                    await using var scope = scopes.CreateAsyncScope();
                    if (!(await scope.ServiceProvider.GetRequiredService<IAuthenticatedSessionReader>()
                        .ReadAsync(entry.Actor, recipient.Token)).Allowed) continue;
                    await scope.ServiceProvider.GetRequiredService<ConversationAccess>().DeliverAsync(entry.Actor, conversationId, null,
                        (_, ct) => hub.Clients.Client(entry.Id).SendAsync(method, payload, ct), recipient.Token, ConversationKind.Group);
                }
                finally { entry.Gate.Release(); }
            }
            catch (OperationCanceledException) { }
            catch (Exception) { logger.LogDebug("Group chat notification was skipped for {EventType}.", method); }
        }
    }
}
