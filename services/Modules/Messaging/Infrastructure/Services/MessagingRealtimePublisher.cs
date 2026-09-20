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

        var inboxEvent = new RealtimeEventEnvelope(
            notification.EventId,
            "SpaceUpdated",
            SchemaVersion: 1,
            notification.SpaceId,
            timeProvider.GetUtcNow(),
            AggregateVersion: null,
            new { });
        var recipientConnections = new HashSet<string>(StringComparer.Ordinal);
        foreach (var memberUserId in await spaceAccess.GetActiveMemberIdsAsync(notification.SpaceId, cancellationToken))
        {
            foreach (var connection in connections.GetUserConnections(memberUserId))
            {
                if (recipientConnections.Add(connection.ConnectionId)
                    && await spaceAccess.GetReadAccessAsync(memberUserId, notification.SpaceId, cancellationToken) is not null)
                {
                    await hubContext.Clients.Client(connection.ConnectionId)
                        .SendAsync("RealtimeEvent", inboxEvent, cancellationToken);
                }
            }
        }
    }
}
