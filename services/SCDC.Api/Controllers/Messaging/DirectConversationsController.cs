using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Controllers.Messaging;

[Authorize]
[Route("api/v1/direct-conversations")]
public sealed class DirectConversationsController(IDirectConversationService service, IConversationInbox inbox) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<DirectConversationPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<DirectConversationPage>> List([FromQuery, Range(1, 50)] int limit = 20,
        [FromQuery] string? cursor = null, CancellationToken cancellationToken = default)
    {
        if (Request.Query.TryGetValue("cursor", out var supplied)) cursor = supplied.ToString();
        return FromResult(await inbox.ListAsync(new(User.GetUserId(), User.GetSessionId(),
            Guid.Parse(User.FindFirst("sst")!.Value), limit, cursor), cancellationToken));
    }

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
