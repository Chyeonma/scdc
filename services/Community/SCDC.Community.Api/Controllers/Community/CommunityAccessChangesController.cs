using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Contracts.Community;

namespace SCDC.Community.Api.Controllers.Community;

[Authorize]
[Route("api/v1/servers/{serverId:guid}/access-operations")]
public sealed class CommunityAccessChangesController(ICommunityChangeStatusReader statuses):ApiControllerBase
{
    [HttpGet("{operationId:guid}")]
    public async Task<ActionResult<CommunityChangeStatus>> Get(Guid serverId,Guid operationId,CancellationToken ct)
    {
        var status=await statuses.ReadAsync(operationId,CommunityHttpActor.Read(this),serverId,ct);
        return status is null?NotFound():Ok(status);
    }
}
