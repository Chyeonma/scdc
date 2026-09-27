using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Controllers.Messaging;

[Authorize]
[Route("api/v1/users/me/blocks")]
public sealed class UserBlocksController(IUserBlockService blocks) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserBlockDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserBlockDto>>> List(CancellationToken ct) =>
        FromResult(await blocks.ListAsync(User.GetUserId(), ct));

    [HttpPut("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Block(Guid userId, CancellationToken ct) =>
        FromNoContentResult(await blocks.BlockAsync(User.GetUserId(), userId, ct));

    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unblock(Guid userId, CancellationToken ct) =>
        FromNoContentResult(await blocks.UnblockAsync(User.GetUserId(), userId, ct));
}
