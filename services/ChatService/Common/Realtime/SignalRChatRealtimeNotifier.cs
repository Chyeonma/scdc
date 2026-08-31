using ChatService.Services.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.Common.Realtime;

public sealed class SignalRChatRealtimeNotifier(
    ChatSubscriptionRegistry subscriptions,
    IHubContext<ChatHub> hubContext) : IChatRealtimeNotifier
{
    public async Task RevokeChannelAccessAsync(
        Guid userId,
        IReadOnlyCollection<Guid> channelIds,
        CancellationToken cancellationToken)
    {
        foreach (var channelId in channelIds)
        {
            var connectionIds = subscriptions.RemoveUserFromChannel(userId, channelId);
            if (connectionIds.Count == 0)
            {
                continue;
            }

            foreach (var connectionId in connectionIds)
            {
                await hubContext.Groups.RemoveFromGroupAsync(
                    connectionId,
                    ChatGroups.Channel(channelId),
                    cancellationToken);
            }

            await hubContext.Clients.Clients(connectionIds).SendAsync(
                "AccessRevoked",
                new { channelId },
                cancellationToken);
        }
    }
}
