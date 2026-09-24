namespace SCDC.Contracts.Messaging;

/// <summary>Messaging-owned lifecycle for a Community channel's chat space.</summary>
public interface IChannelSpaceProvisioner
{
    Task<ChannelSpaceProvisionResult> CreateAsync(Guid createdByUserId, CancellationToken cancellationToken);
    Task ArchiveAsync(Guid spaceId, CancellationToken cancellationToken);
    Task RetireAsync(Guid spaceId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, short>> GetStatusesAsync(
        IReadOnlyCollection<Guid> spaceIds,
        CancellationToken cancellationToken);
}

public sealed record ChannelSpaceProvisionResult(Guid? SpaceId, string? FailureReason = null)
{
    public bool IsSuccess => SpaceId is { } id && id != Guid.Empty;
}
