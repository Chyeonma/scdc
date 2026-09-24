using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Controllers.Messaging;

[Authorize]
[Route("api/v1/spaces/{spaceId:guid}/preferences")]
public sealed class SpacePreferencesController(ISpacePreferencesService preferencesService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<UserSpacePreferencesDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSpacePreferencesDto>> Get(Guid spaceId, CancellationToken cancellationToken) =>
        FromResult(await preferencesService.GetAsync(User.GetUserId(), spaceId, cancellationToken));

    [HttpPut]
    [ProducesResponseType<UserSpacePreferencesDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSpacePreferencesDto>> Update(
        Guid spaceId,
        UserSpacePreferencesDto preferences,
        CancellationToken cancellationToken) =>
        FromResult(await preferencesService.UpdateAsync(User.GetUserId(), spaceId, preferences, cancellationToken));
}
