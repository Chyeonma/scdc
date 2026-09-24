using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Messaging;

namespace SCDC.Modules.Messaging.Application;

public interface ISpacePreferencesService
{
    Task<Result<UserSpacePreferencesDto>> GetAsync(Guid actorUserId, Guid spaceId, CancellationToken cancellationToken);
    Task<Result<UserSpacePreferencesDto>> UpdateAsync(
        Guid actorUserId,
        Guid spaceId,
        UserSpacePreferencesDto? preferences,
        CancellationToken cancellationToken);
}
