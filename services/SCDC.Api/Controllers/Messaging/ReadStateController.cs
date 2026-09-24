using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Controllers.Messaging;

[Authorize]
[Route("api/v1/spaces/{spaceId:guid}/read-state")]
public sealed class ReadStateController(IReadStateService readStateService) : ApiControllerBase
{
    [HttpPut]
    [ProducesResponseType<ReadStateDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReadStateDto>> Update(
        Guid spaceId,
        UpdateReadStateRequest request,
        CancellationToken cancellationToken) => FromResult(await readStateService.UpdateAsync(
            User.GetUserId(), spaceId, request.LastReadSequence, cancellationToken));
}

public sealed record UpdateReadStateRequest(string? LastReadSequence);
