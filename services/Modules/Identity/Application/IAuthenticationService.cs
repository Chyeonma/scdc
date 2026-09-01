using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Identity.Application;

public interface IAuthenticationService
{
    Task<Result<AuthResponse>> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken);

    Task<Result<AuthResponse>> RefreshAsync(
        RefreshSessionCommand command,
        CancellationToken cancellationToken);

    Task<Result> LogoutAsync(
        LogoutCommand command,
        CancellationToken cancellationToken);

    Task<Result> LogoutAllAsync(
        Guid userId,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<SessionResponse>>> GetSessionsAsync(
        Guid userId,
        Guid currentSessionId,
        CancellationToken cancellationToken);

    Task<Result> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        RequestContext context,
        CancellationToken cancellationToken);
}
