using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Messaging.Application;

public sealed record UserBlockDto(Guid UserId, string? Username, string? DisplayName, DateTimeOffset CreatedAt);

public interface IUserBlockService
{
    Task<Result<IReadOnlyList<UserBlockDto>>> ListAsync(Guid actorUserId, CancellationToken cancellationToken);
    Task<Result> BlockAsync(Guid actorUserId, Guid targetUserId, CancellationToken cancellationToken);
    Task<Result> UnblockAsync(Guid actorUserId, Guid targetUserId, CancellationToken cancellationToken);
}
