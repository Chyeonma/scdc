using ChatService.Common.Auth;
using ChatService.Services.Abstractions;

namespace ChatService.Common.Auth;

public sealed class HttpRequestContext(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserContext, IRequestContext
{
    private HttpContext HttpContext => httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("No active HTTP context is available.");

    public Guid UserId => HttpContext.User.GetRequiredUserId();
    public System.Net.IPAddress? ClientIp => HttpContext.Connection.RemoteIpAddress;
}
