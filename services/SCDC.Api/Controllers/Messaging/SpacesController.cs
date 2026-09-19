using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Controllers.Messaging;

[Authorize]
[Route("api/v1/spaces")]
public sealed class SpacesController(IDirectConversationService directConversationService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<SpacePageDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SpacePageDto>> List(
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromQuery] bool includeHidden,
        CancellationToken cancellationToken)
    {
        var result = await directConversationService.ListAsync(
            new ListSpacesQuery(
                User.GetUserId(),
                limit ?? 50,
                cursor,
                includeHidden),
            cancellationToken);
        return FromResult(result);
    }

    [HttpGet("{spaceId:guid}")]
    [ProducesResponseType<SpaceSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SpaceSummaryDto>> GetById(
        Guid spaceId,
        CancellationToken cancellationToken)
    {
        var result = await directConversationService.GetSpaceAsync(
            User.GetUserId(),
            spaceId,
            cancellationToken);
        return FromResult(result);
    }
}
