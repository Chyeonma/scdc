using SCDC.Media.Domain;

namespace SCDC.Media.Application;

public interface IIdentityAuthority
{
    Task<PublicUser> CheckAsync(string accessToken, CancellationToken ct);
}
public interface ICallStore
{
    Task<CallRecord?> CurrentAsync(Guid user, CancellationToken ct);
    Task<CallRecord> GetAsync(Guid id, CancellationToken ct);
    Task<CallRecord> CreateAsync(MediaActor caller, MediaActor peer, Guid operation, CancellationToken ct);
    Task<CallRecord> ActAsync(Guid id, MediaActor actor, string action, string version, Guid operation, CancellationToken ct);
    Task<IReadOnlyList<CallRecord>> PendingAsync(CancellationToken ct);
    Task<CallRecord> EndAsync(Guid id, string reason, CancellationToken ct);
    Task ClosedAsync(Guid id, CancellationToken ct);
    Task RecoverAsync(CancellationToken ct);
}
public interface IMediaProvider
{
    Task CreateAsync(CallRecord call, CancellationToken ct);
    Task DeleteAsync(CallRecord call, CancellationToken ct);
    string Token(CallRecord call, MediaActor actor);
    bool Validate(string token, out Guid callId, out Guid userId, out Guid sessionId, out Guid instanceId);
}
