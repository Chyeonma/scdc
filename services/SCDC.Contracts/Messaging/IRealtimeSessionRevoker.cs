namespace SCDC.Contracts.Messaging;

public interface IRealtimeSessionRevoker
{
    Task RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken);

    Task RevokeUserAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
