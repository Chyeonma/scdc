using System.Text.Json;
using ChatService.Domain.Entities;

namespace ChatService.Common.Messaging;

internal static class OutboxFactory
{
    public static OutboxEvent Create(
        string eventType,
        Guid aggregateId,
        int aggregateVersion,
        Guid channelId,
        object payload,
        DateTimeOffset occurredAt) =>
        new()
        {
            EventType = eventType,
            AggregateId = aggregateId,
            AggregateVersion = aggregateVersion,
            ChannelId = channelId,
            Payload = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            OccurredAt = occurredAt,
            AvailableAt = occurredAt
        };
}

internal static class ChatEventTypes
{
    public const string MessageCreated = nameof(MessageCreated);
    public const string MessageUpdated = nameof(MessageUpdated);
    public const string MessageDeleted = nameof(MessageDeleted);
}
