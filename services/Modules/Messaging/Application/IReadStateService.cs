using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Messaging.Application;

public interface IReadStateService
{
    Task<Result<ReadStateDto>> UpdateAsync(
        Guid actorUserId,
        Guid spaceId,
        string? lastReadSequence,
        CancellationToken cancellationToken);
}

public sealed record ReadStateDto(Guid SpaceId, string LastReadSequence, DateTimeOffset LastReadAt);
