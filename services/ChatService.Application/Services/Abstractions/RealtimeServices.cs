using System.Text.Json;

namespace ChatService.Services.Abstractions;

public interface IChatRealtimeNotifier
{
    Task RevokeChannelAccessAsync(
        Guid userId,
        IReadOnlyCollection<Guid> channelIds,
        CancellationToken cancellationToken);
}

public interface IRealtimeEventSender
{
    Task SendToChannelAsync(
        Guid channelId,
        string eventType,
        JsonElement payload,
        CancellationToken cancellationToken);
}
