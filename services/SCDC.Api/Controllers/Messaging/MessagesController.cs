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
                request.Content,
                request.ReplyToMessageId,
                request.ThreadRootId),
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

    [HttpGet("{messageId:guid}/replies")]
    [ProducesResponseType<MessagePageDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MessagePageDto>> GetReplies(
        Guid spaceId, Guid messageId, [FromQuery] int? limit,
        [FromQuery] string? beforeSequence, [FromQuery] string? afterSequence,
        [FromQuery] string? throughSequence, CancellationToken cancellationToken) =>
        FromResult(await messageService.GetThreadRepliesAsync(new GetThreadRepliesQuery(
            User.GetUserId(), spaceId, messageId, limit ?? 50,
            beforeSequence, afterSequence, throughSequence), cancellationToken));

    [HttpGet("{messageId:guid}")]
    [ProducesResponseType<MessageDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageDto>> Get(Guid spaceId, Guid messageId, CancellationToken cancellationToken) =>
        FromResult(await messageService.GetAsync(User.GetUserId(), spaceId, messageId, cancellationToken));

    [HttpPatch("{messageId:guid}")]
    [ProducesResponseType<MessageDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageDto>> Edit(
        Guid spaceId, Guid messageId, EditMessageRequest request, CancellationToken cancellationToken) =>
        FromResult(await messageService.EditAsync(
            new EditMessageCommand(User.GetUserId(), spaceId, messageId, request.Content, request.ExpectedVersion),
            cancellationToken));

    [HttpDelete("{messageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid spaceId, Guid messageId, [FromQuery] int expectedVersion, CancellationToken cancellationToken)
    {
        var result = await messageService.DeleteAsync(
            new DeleteMessageCommand(User.GetUserId(), spaceId, messageId, expectedVersion), cancellationToken);
        return FromNoContentResult(result);
    }
}

public sealed record SendMessageRequest(
    Guid ClientMessageId,
    short MessageType,
    string? Content,
    Guid? ReplyToMessageId,
    Guid? ThreadRootId);

public sealed record EditMessageRequest(string? Content, int ExpectedVersion);
