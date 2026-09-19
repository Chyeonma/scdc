using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Messaging.Application;

public interface IDirectConversationService
{
    Task<Result<CreateDirectConversationResult>> GetOrCreateAsync(
        CreateDirectConversationCommand command,
        CancellationToken cancellationToken);

    Task<Result<SpaceSummaryDto>> GetSpaceAsync(
        Guid actorUserId,
        Guid spaceId,
        CancellationToken cancellationToken);
}
