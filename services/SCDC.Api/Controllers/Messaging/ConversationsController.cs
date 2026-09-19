using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Controllers.Messaging;

[Authorize]
[Route("api/v1/conversations")]
public sealed class ConversationsController(IDirectConversationService directConversationService) : ApiControllerBase
{
    [HttpGet("direct/recipient")]
    [ProducesResponseType<SCDC.Contracts.Identity.UserSummary>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SCDC.Contracts.Identity.UserSummary>> FindDirectRecipient(
        [FromQuery, Required, StringLength(32, MinimumLength = 3)] string username,
        CancellationToken cancellationToken)
    {
        var result = await directConversationService.FindRecipientByUsernameAsync(
            User.GetUserId(),
            username,
            cancellationToken);
        return FromResult(result);
    }

    [HttpPost("direct")]
    [ProducesResponseType<SpaceSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<SpaceSummaryDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<SpaceSummaryDto>> GetOrCreateDirect(
        CreateDirectConversationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await directConversationService.GetOrCreateAsync(
            new CreateDirectConversationCommand(User.GetUserId(), request.RecipientUserId),
            cancellationToken);
        if (result.IsFailure)
        {
            return FromResult(Result.Failure<SpaceSummaryDto>(result.Error));
        }

        var conversation = result.Value;
        return conversation.Created
            ? Created($"/api/v1/spaces/{conversation.Space.Id}", conversation.Space)
            : Ok(conversation.Space);
    }
}

public sealed record CreateDirectConversationRequest(
    [param: Required] Guid RecipientUserId);
