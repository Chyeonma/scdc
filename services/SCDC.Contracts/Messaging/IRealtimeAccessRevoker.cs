namespace SCDC.Contracts.Messaging;

public interface IRealtimeAccessRevoker
{
    Task RevokeAsync(
        Guid userId,
        IReadOnlyCollection<Guid> spaceIds,
        CancellationToken cancellationToken);
}
