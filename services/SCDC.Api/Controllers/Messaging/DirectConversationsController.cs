using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Controllers.Messaging;

[Authorize]
[Route("api/v1/direct-conversations")]
public sealed class DirectConversationsController(IDirectConversationService service) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<DirectConversationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<DirectConversationResponse>> Open(OpenDirectConversationRequest request,
        CancellationToken cancellationToken) => FromResult(await service.OpenAsync(new OpenDirectConversation(
            User.GetUserId(), User.GetSessionId(), Guid.Parse(User.FindFirst("sst")!.Value), request.PeerUserId!.Value), cancellationToken));
}

public sealed record OpenDirectConversationRequest([Required] Guid? PeerUserId);
