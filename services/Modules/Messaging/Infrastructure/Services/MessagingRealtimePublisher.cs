using Microsoft.AspNetCore.SignalR;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Hubs;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class MessagingRealtimePublisher(
    IHubContext<ChatHub> hubContext,
    RealtimeConnectionRegistry connections,
    IRealtimeSpaceAccess spaceAccess,
    TimeProvider timeProvider) : IRealtimeMessagePublisher
{
    public async Task PublishMessageChangedAsync(RealtimeMessageChanged notification, CancellationToken cancellationToken)
    {
        if (notification.EventId == Guid.Empty || notification.SpaceId == Guid.Empty || notification.MessageId == Guid.Empty)
            return;
        var envelope = new RealtimeEventEnvelope(
            notification.EventId, notification.DeletedAt is null ? "MessageUpdated" : "MessageDeleted",
            SchemaVersion: 1, notification.SpaceId, notification.OccurredAt,
            notification.AggregateVersion,
            notification.DeletedAt is null
                ? new { notification.MessageId, notification.SequenceNo }
                : (object)new { notification.MessageId, notification.SequenceNo, notification.DeletedAt });
        foreach (var connection in connections.GetSubscribers(notification.SpaceId))
        {
            if (await spaceAccess.GetReadAccessAsync(connection.UserId, notification.SpaceId, cancellationToken) is not null)
                await hubContext.Clients.Client(connection.ConnectionId).SendAsync("RealtimeEvent", envelope, cancellationToken);
        }
        await PublishSpaceUpdatedAsync(notification.EventId, notification.SpaceId, cancellationToken);
    }

    public async Task PublishMessageCreatedAsync(
        RealtimeMessageCreated notification,
        CancellationToken cancellationToken)
    {
        if (notification.EventId == Guid.Empty || notification.SpaceId == Guid.Empty || notification.MessageId == Guid.Empty)
        {
            return;
        }

        var messageEvent = new RealtimeEventEnvelope(
            notification.EventId,
            "MessageCreated",
            SchemaVersion: 1,
            notification.SpaceId,
            notification.OccurredAt,
            notification.AggregateVersion,
            new MessageCreatedPayload(notification.MessageId, notification.SequenceNo));

        foreach (var connection in connections.GetSubscribers(notification.SpaceId))
        {
            // Dispatch rechecks current authorization: joining a group never grants durable delivery rights.
            if (await spaceAccess.GetReadAccessAsync(connection.UserId, notification.SpaceId, cancellationToken) is null)
            {
                continue;
            }

            await hubContext.Clients.Client(connection.ConnectionId)
                .SendAsync("RealtimeEvent", messageEvent, cancellationToken);
        }

        await PublishSpaceUpdatedAsync(notification.EventId, notification.SpaceId, cancellationToken);
    }

    private async Task PublishSpaceUpdatedAsync(Guid eventId, Guid spaceId, CancellationToken cancellationToken)
    {
        var inboxEvent = new RealtimeEventEnvelope(
            eventId,
            "SpaceUpdated",
            SchemaVersion: 1,
            spaceId,
            timeProvider.GetUtcNow(),
            AggregateVersion: null,
            new { });
        var recipientConnections = new HashSet<string>(StringComparer.Ordinal);
        foreach (var memberUserId in await spaceAccess.GetActiveMemberIdsAsync(spaceId, cancellationToken))
        {
            foreach (var connection in connections.GetUserConnections(memberUserId))
            {
                if (recipientConnections.Add(connection.ConnectionId)
                    && await spaceAccess.GetReadAccessAsync(memberUserId, spaceId, cancellationToken) is not null)
                {
                    await hubContext.Clients.Client(connection.ConnectionId)
                        .SendAsync("RealtimeEvent", inboxEvent, cancellationToken);
                }
            }
        }
    }
}
