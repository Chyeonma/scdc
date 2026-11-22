using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SCDC.Media.Application;
using SCDC.Media.Domain;

namespace SCDC.Media.Api;

[Authorize]
public sealed class MediaHub(MediaRegistry registry, IIdentityAuthority authority) : Hub
{
    public async Task<object> RegisterMediaClient(Guid clientInstanceId)
    {
        try
        {
            CallRules.Uuid4(clientInstanceId);
            var http = Context.GetHttpContext()!;
            var token = http.Request.Headers.Authorization.ToString();
            token = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? token[7..] : http.Request.Query["access_token"].ToString();
            var user = await authority.CheckAsync(token, Context.ConnectionAborted);
            var claims = Context.User!;
            if (user.Id != Guid.Parse(claims.FindFirstValue("sub")!)) throw new MediaProblem(401, "Media.SessionInvalid", "Invalid session.");
            registry.Register(Context.ConnectionId, new(user.Id, Guid.Parse(claims.FindFirstValue("sid")!), clientInstanceId,
                token, user, DateTimeOffset.FromUnixTimeSeconds(long.Parse(claims.FindFirstValue("exp")!))));
            return new { registered = true, clientInstanceId };
        }
        catch (MediaProblem problem) { throw new HubException(problem.Code); }
    }
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        registry.Disconnected(Context.ConnectionId); return base.OnDisconnectedAsync(exception);
    }
}
public sealed class MediaUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection.User?.FindFirstValue("sub");
}
