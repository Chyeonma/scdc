using System.Net;

namespace ChatService.Services.Abstractions;

public interface ICurrentUserContext
{
    Guid UserId { get; }
}

public interface IRequestContext
{
    IPAddress? ClientIp { get; }
}
