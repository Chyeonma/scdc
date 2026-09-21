namespace SCDC.Modules.Messaging.Application;

public interface IRealtimeSpaceAccess
{
    Task<RealtimeSpaceReadAccess?> GetReadAccessAsync(
        Guid userId,
        Guid spaceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetActiveMemberIdsAsync(
        Guid spaceId,
        CancellationToken cancellationToken);
}

public sealed record RealtimeSpaceReadAccess(Guid SpaceId, string HighWatermark);
