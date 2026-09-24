using Microsoft.AspNetCore.SignalR;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Messaging.Hubs;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class MessagingRealtimeAccessRevoker(
    IHubContext<ChatHub> hubContext,
    RealtimeConnectionRegistry connections,
    TimeProvider timeProvider) : IRealtimeAccessRevoker, IRealtimeSessionRevoker
{
    public async Task RevokeAsync(
        Guid userId,
        IReadOnlyCollection<Guid> spaceIds,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || spaceIds.Count == 0)
        {
            return;
        }

        var revokedSpaceIds = spaceIds.Where(spaceId => spaceId != Guid.Empty).ToHashSet();
        if (revokedSpaceIds.Count == 0)
        {
            return;
        }

        foreach (var connection in connections.GetUserConnections(userId))
        {
            foreach (var spaceId in revokedSpaceIds)
            {
                if (!connections.Unsubscribe(connection.ConnectionId, spaceId))
                {
                    continue;
                }

                await SendIgnoringDisconnectAsync(
                    connection.ConnectionId,
                    CreateEvent("SpaceAccessRevoked", spaceId, null, new { }),
                    cancellationToken);
                await RemoveFromGroupIgnoringDisconnectAsync(connection.ConnectionId, spaceId, cancellationToken);
            }
        }
    }

    public async Task NotifySpaceUpdatedAsync(
        IReadOnlyCollection<Guid> userIds,
        Guid spaceId,
        CancellationToken cancellationToken)
    {
        if (spaceId == Guid.Empty) return;
        foreach (var userId in userIds.Where(userId => userId != Guid.Empty).Distinct())
        {
            foreach (var connection in connections.GetUserConnections(userId))
            {
                await SendIgnoringDisconnectAsync(
                    connection.ConnectionId,
                    CreateEvent("SpaceUpdated", spaceId, null, new { }),
                    cancellationToken);
            }
        }
    }

    public async Task NotifyPreferencesUpdatedAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || spaceId == Guid.Empty) return;
        foreach (var connection in connections.GetUserConnections(userId))
        {
            await SendIgnoringDisconnectAsync(
                connection.ConnectionId,
                CreateEvent("PreferencesUpdated", spaceId, null, new { }),
                cancellationToken);
        }
    }

    public async Task RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || sessionId == Guid.Empty)
        {
            return;
        }

        var targets = connections.GetSessionConnections(userId, sessionId);
        foreach (var connection in targets)
        {
            connections.Remove(connection.ConnectionId);
            await SendIgnoringDisconnectAsync(
                connection.ConnectionId,
                CreateEvent("SessionRevoked", null, null, new { }),
                cancellationToken);
            connection.Abort();
        }
    }

    public async Task RevokeUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        var targets = connections.GetUserConnections(userId);
        foreach (var connection in targets)
        {
            connections.Remove(connection.ConnectionId);
            await SendIgnoringDisconnectAsync(
                connection.ConnectionId,
                CreateEvent("SessionRevoked", null, null, new { }),
                cancellationToken);
            connection.Abort();
        }
    }

    private RealtimeEventEnvelope CreateEvent(
        string eventType,
        Guid? spaceId,
        int? aggregateVersion,
        object payload) => new(
        Guid.CreateVersion7(),
        eventType,
        SchemaVersion: 1,
        spaceId,
        timeProvider.GetUtcNow(),
        aggregateVersion,
        payload);

    private async Task SendIgnoringDisconnectAsync(
        string connectionId,
        RealtimeEventEnvelope realtimeEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            await hubContext.Clients.Client(connectionId)
                .SendAsync("RealtimeEvent", realtimeEvent, cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // A disconnected transport is already revoked; state was removed before this best-effort notification.
        }
    }

    private async Task RemoveFromGroupIgnoringDisconnectAsync(
        string connectionId,
        Guid spaceId,
        CancellationToken cancellationToken)
    {
        try
        {
            await hubContext.Groups.RemoveFromGroupAsync(
                connectionId,
                ChatHub.GroupName(spaceId),
                cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Connection teardown removes all SignalR group memberships.
        }
    }
}
