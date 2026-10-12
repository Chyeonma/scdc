using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Messaging.Realtime;

public sealed record SubscriptionAcknowledgement(Guid ConversationId, bool Subscribed);

[Authorize]
public sealed class ChatHub(ConversationRegistry registry, IServiceScopeFactory scopes) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var p = Context.User;
        if (!Guid.TryParse(p?.FindFirst("sub")?.Value, out var user)
            || !Guid.TryParse(p?.FindFirst("sid")?.Value, out var sid)
            || !Guid.TryParse(p?.FindFirst("sst")?.Value, out var stamp)
            || !long.TryParse(p?.FindFirst("exp")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var exp))
            throw Problem(401, "Common.Unauthorized");
        var actor = new AuthenticatedSession(user, sid, stamp, DateTimeOffset.FromUnixTimeSeconds(exp));
        await CheckSession(actor);
        var context = Context;
        registry.Add(new(context.ConnectionId, actor, context.ConnectionAborted, context.Abort));
        await base.OnConnectedAsync();
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await registry.RemoveAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
    public async Task<SubscriptionAcknowledgement> SubscribeConversation(JsonElement payload)
    {
        var space = Parse(payload); var entry = Entry();
        await entry.Gate.WaitAsync(Context.ConnectionAborted);
        try
        {
            if (entry.Closed) throw Problem(401, "Common.Unauthorized");
            await CheckSession(entry.Actor);
            await using var scope = scopes.CreateAsyncScope();
            var access = await scope.ServiceProvider.GetRequiredService<ConversationAccess>()
                .AuthorizeAsync(entry.Actor, space, Context.ConnectionAborted);
            if (access.IsFailure) throw Problem(access.Error.Code == "Common.Unauthorized" ? 401 : access.Error.Code == "RESOURCE_NOT_FOUND" ? 404 : 503, access.Error.Code);
            entry.Spaces.Add(space);
            return new(space, true);
        }
        finally { entry.Gate.Release(); }
    }
    public async Task<SubscriptionAcknowledgement> UnsubscribeConversation(JsonElement payload)
    {
        var space = Parse(payload); var entry = Entry();
        await entry.Gate.WaitAsync(Context.ConnectionAborted);
        try
        {
            if (entry.Closed) throw Problem(401, "Common.Unauthorized");
            await CheckSession(entry.Actor); entry.Spaces.Remove(space); return new(space, false);
        }
        finally { entry.Gate.Release(); }
    }
    private ConversationRegistry.Connection Entry() => registry.Find(Context.ConnectionId) ?? throw Problem(401, "Common.Unauthorized");
    private async Task CheckSession(AuthenticatedSession actor)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            if (!(await scope.ServiceProvider.GetRequiredService<IAuthenticatedSessionReader>().ReadAsync(actor, Context.ConnectionAborted)).Allowed)
                throw Problem(401, "Common.Unauthorized");
        }
        catch (Exception e) when (DatabaseAvailability.IsUnavailable(e)) { throw Problem(503, "AUTHORITY_UNAVAILABLE"); }
    }
    private static Guid Parse(JsonElement p)
    {
        if (p.ValueKind != JsonValueKind.Object || p.EnumerateObject().Count() != 1 || !p.TryGetProperty("conversationId", out var id)
            || id.ValueKind != JsonValueKind.String || !id.TryGetGuid(out var space) || space == Guid.Empty)
            throw Problem(400, "Common.ValidationFailed");
        return space;
    }
    public static HubException Problem(int status, string code) => new(JsonSerializer.Serialize(new
    {
        type = "about:blank", title = status == 401 ? "Unauthorized" : status == 404 ? "Not Found" : status == 400 ? "Bad Request" : "Service Unavailable",
        status, detail = "The requested subscription is not available.", errorCode = code
    }));
}
