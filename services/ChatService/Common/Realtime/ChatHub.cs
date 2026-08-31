using ChatService.Common.Auth;
using ChatService.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.Common.Realtime;

[Authorize]
public sealed class ChatHub(
    IChannelRepository channels,
    ChatSubscriptionRegistry subscriptions) : Hub
{
    public async Task SubscribeChannel(Guid channelId)
    {
        var userId = Context.User?.GetRequiredUserId()
            ?? throw new HubException("Authentication is required.");
        var canAccess = await channels.CanAccessAsync(
            channelId,
            userId,
            Context.ConnectionAborted);
        if (!canAccess)
        {
            throw new HubException("Channel not found or access was denied.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            ChatGroups.Channel(channelId),
            Context.ConnectionAborted);
        subscriptions.Add(userId, channelId, Context.ConnectionId);
    }

    public async Task UnsubscribeChannel(Guid channelId)
    {
        var userId = Context.User?.GetRequiredUserId()
            ?? throw new HubException("Authentication is required.");
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            ChatGroups.Channel(channelId),
            Context.ConnectionAborted);
        subscriptions.Remove(userId, channelId, Context.ConnectionId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        subscriptions.RemoveConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
