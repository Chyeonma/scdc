using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Controllers.Messaging;

[Authorize]
[Route("api/v1/spaces/{spaceId:guid}/messages")]
public sealed class MessagesController(IMessageService messageService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<MessagePageDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MessagePageDto>> GetHistory(
        Guid spaceId,
        [FromQuery] int? limit,
        [FromQuery] string? beforeSequence,
        [FromQuery] string? afterSequence,
        [FromQuery] string? throughSequence,
        CancellationToken cancellationToken)
    {
        var result = await messageService.GetHistoryAsync(
            new GetMessagesQuery(
                User.GetUserId(),
                spaceId,
                limit ?? 50,
                beforeSequence,
                afterSequence,
                throughSequence),
            cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    [ProducesResponseType<MessageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MessageDto>> Send(
        Guid spaceId,
        SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await messageService.SendAsync(
            new SendMessageCommand(
                User.GetUserId(),
                spaceId,
                request.ClientMessageId,
                request.MessageType,
                request.Content),
            cancellationToken);
        if (result.IsFailure)
        {
            if (result.Error == MessagingErrors.RateLimited)
            {
                Response.Headers.RetryAfter = "60";
            }

            return FromResult(Result.Failure<MessageDto>(result.Error));
        }

        var sent = result.Value;
        return sent.Created
            ? Created($"/api/v1/spaces/{spaceId}/messages/{sent.Message.Id}", sent.Message)
            : Ok(sent.Message);
    }
}

public sealed record SendMessageRequest(
    Guid ClientMessageId,
    short MessageType,
    string? Content);
