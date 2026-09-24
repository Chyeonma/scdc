using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Modules.Messaging.Hubs;

[Authorize]
public sealed class ChatHub(
    IIdentitySessionValidator sessionValidator,
    IRealtimeSpaceAccess spaceAccess,
    ITypingAccess typingAccess,
    IUserDirectory userDirectory,
    RealtimeConnectionRegistry connections,
    TypingStateRegistry typingStates,
    TimeProvider timeProvider) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (!await TryValidateSessionAsync(Context.ConnectionAborted))
        {
            Context.Abort();
            return;
        }

        var (userId, sessionId, _) = GetIdentityClaims();
        connections.Register(userId, sessionId, Context.ConnectionId, Context.Abort);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        typingStates.RemoveConnection(Context.ConnectionId);
        connections.Remove(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<HubResult<SubscribeSpaceResponse>> SubscribeSpace(Guid spaceId)
    {
        if (spaceId == Guid.Empty)
        {
            return Failure<SubscribeSpaceResponse>("Messaging.ValidationFailed", "Space ID is invalid.");
        }

        if (!await TryValidateSessionAsync(Context.ConnectionAborted))
        {
            Context.Abort();
            return Failure<SubscribeSpaceResponse>("Identity.SessionInvalid", "The session is no longer active.");
        }

        var (userId, _, _) = GetIdentityClaims();
        var access = await spaceAccess.GetReadAccessAsync(userId, spaceId, Context.ConnectionAborted);
        if (access is null)
        {
            return Failure<SubscribeSpaceResponse>("Messaging.ResourceNotFound", "The space was not found.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(spaceId), Context.ConnectionAborted);
        connections.Subscribe(Context.ConnectionId, spaceId);

        // Recheck after the group mutation so a concurrent membership revoke cannot leave a usable subscription.
        if (!connections.IsSubscribed(Context.ConnectionId, spaceId)
            || await spaceAccess.GetReadAccessAsync(userId, spaceId, Context.ConnectionAborted) is null)
        {
            connections.Unsubscribe(Context.ConnectionId, spaceId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(spaceId), Context.ConnectionAborted);
            return Failure<SubscribeSpaceResponse>("Messaging.ResourceNotFound", "The space was not found.");
        }

        return HubResult<SubscribeSpaceResponse>.Success(new SubscribeSpaceResponse(spaceId, access.HighWatermark));
    }

    public async Task<HubResult<UnsubscribeSpaceResponse>> UnsubscribeSpace(Guid spaceId)
    {
        if (spaceId == Guid.Empty)
        {
            return Failure<UnsubscribeSpaceResponse>("Messaging.ValidationFailed", "Space ID is invalid.");
        }

        if (!await TryValidateSessionAsync(Context.ConnectionAborted))
        {
            Context.Abort();
            return Failure<UnsubscribeSpaceResponse>("Identity.SessionInvalid", "The session is no longer active.");
        }

        connections.Unsubscribe(Context.ConnectionId, spaceId);
        typingStates.Stop(Context.ConnectionId, GetIdentityClaims().UserId, spaceId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(spaceId), Context.ConnectionAborted);
        return HubResult<UnsubscribeSpaceResponse>.Success(new UnsubscribeSpaceResponse(spaceId));
    }

    public async Task<HubResult<TypingResponse>> SetTyping(Guid spaceId, bool isTyping)
    {
        if (spaceId == Guid.Empty || !connections.IsSubscribed(Context.ConnectionId, spaceId))
            return Failure<TypingResponse>("Messaging.ResourceNotFound", "The space is not subscribed.");
        if (!await TryValidateSessionAsync(Context.ConnectionAborted))
        {
            Context.Abort();
            return Failure<TypingResponse>("Identity.SessionInvalid", "The session is no longer active.");
        }

        var (userId, _, _) = GetIdentityClaims();
        if (!await typingAccess.CanSendAsync(userId, spaceId, Context.ConnectionAborted))
            return Failure<TypingResponse>("Messaging.ActionNotAllowed", "Typing is not allowed in this space.");

        DateTimeOffset? expiresAt = null;
        bool publish;
        if (isTyping)
        {
            publish = typingStates.Start(Context.ConnectionId, userId, spaceId, out var expiry);
            expiresAt = expiry;
        }
        else
        {
            publish = typingStates.Stop(Context.ConnectionId, userId, spaceId);
        }

        if (publish)
        {
            var actor = await userDirectory.FindByIdAsync(userId, Context.ConnectionAborted);
            if (actor is not null)
            {
                var envelope = new RealtimeEventEnvelope(
                    Guid.CreateVersion7(), "TypingChanged", 1, spaceId,
                    timeProvider.GetUtcNow(), null,
                    new TypingChangedPayload(userId, actor.DisplayName, isTyping, expiresAt));
                foreach (var recipient in connections.GetSubscribers(spaceId).Where(item => item.UserId != userId))
                {
                    if (await typingAccess.CanReadAsync(recipient.UserId, spaceId, Context.ConnectionAborted))
                        await Clients.Client(recipient.ConnectionId).SendAsync("RealtimeEvent", envelope, Context.ConnectionAborted);
                }
            }
        }

        return HubResult<TypingResponse>.Success(new TypingResponse(spaceId, isTyping, expiresAt));
    }

    internal static string GroupName(Guid spaceId) => $"space:{spaceId:D}";

    private async Task<bool> TryValidateSessionAsync(CancellationToken cancellationToken)
    {
        if (!TryGetIdentityClaims(out var userId, out var sessionId, out var securityStamp))
        {
            return false;
        }

        var validation = await sessionValidator.ValidateAsync(userId, sessionId, securityStamp, cancellationToken);
        return validation.IsValid;
    }

    private (Guid UserId, Guid SessionId, Guid SecurityStamp) GetIdentityClaims()
    {
        _ = TryGetIdentityClaims(out var userId, out var sessionId, out var securityStamp);
        return (userId, sessionId, securityStamp);
    }

    private bool TryGetIdentityClaims(out Guid userId, out Guid sessionId, out Guid securityStamp)
    {
        var hasUserId = Guid.TryParse(Context.User?.FindFirstValue("sub"), out userId);
        var hasSessionId = Guid.TryParse(Context.User?.FindFirstValue("sid"), out sessionId);
        var hasSecurityStamp = Guid.TryParse(Context.User?.FindFirstValue("sst"), out securityStamp);
        return hasUserId && hasSessionId && hasSecurityStamp;
    }

    private HubResult<T> Failure<T>(string errorCode, string message) =>
        HubResult<T>.Failure(errorCode, message, Activity.Current?.TraceId.ToString() ?? Context.ConnectionId);
}
