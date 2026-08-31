using System.Text.Json;
using ChatService.Services.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.Common.Realtime;

public sealed class SignalRRealtimeEventSender(IHubContext<ChatHub> hubContext)
    : IRealtimeEventSender
{
    public Task SendToChannelAsync(
        Guid channelId,
        string eventType,
        JsonElement payload,
        CancellationToken cancellationToken) =>
        hubContext.Clients
            .Group(ChatGroups.Channel(channelId))
            .SendAsync(eventType, payload, cancellationToken);
}
