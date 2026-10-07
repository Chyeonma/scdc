using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Errors;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Features.Memberships.Application;
using SCDC.Modules.Community.Features.Servers.Application;

namespace SCDC.Api.Controllers.Community;

[Authorize]
[Route("api/v1/servers/{serverId:guid}")]
public sealed class MembershipsController(IMembershipService memberships) : ApiControllerBase
{
    [HttpPost("join")]
    [RequestSizeLimit(1024)]
    [ProducesResponseType(typeof(MembershipView), 200)]
    public async Task<ActionResult<MembershipView>> Join(Guid serverId, CancellationToken ct)
    {
        if (await Request.Body.ReadAsync(new byte[1], ct) != 0)
            return ApiErrorMapper.ToObjectResult(Error.Validation("VALIDATION_FAILED", "Join does not accept a request body."), HttpContext);
        var actor = new AccountActor(Claim("sub"), Claim("sid"), Claim("sst"));
        return FromResult(await memberships.JoinAsync(actor, serverId, ct));
    }

    private Guid Claim(string name) => Guid.TryParse(User.FindFirst(name)?.Value, out var id) ? id
        : throw new InvalidOperationException($"Required claim '{name}' is missing.");
}
