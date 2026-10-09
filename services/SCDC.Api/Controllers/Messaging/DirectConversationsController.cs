using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Controllers.Messaging;

[Authorize]
[Route("api/v1/direct-conversations")]
public sealed class DirectConversationsController(IDirectConversationService service, IConversationInbox inbox, ITextMessageSender sender, IMessageHistory history) : ApiControllerBase
{
    [HttpGet("{conversationId:guid}/messages")]
    [ProducesResponseType<MessagePage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MessagePage>> History(Guid conversationId, [FromQuery, Range(1, 100)] int limit = 50,
        [FromQuery] string? before = null, [FromQuery] string? after = null, [FromQuery] string? through = null,
        CancellationToken cancellationToken = default)
    {
        if (Request.Query.ContainsKey("before")) before = Request.Query["before"].ToString();
        if (Request.Query.ContainsKey("after")) after = Request.Query["after"].ToString();
        if (Request.Query.ContainsKey("through")) through = Request.Query["through"].ToString();
        return FromResult(await history.ReadAsync(new(User.GetUserId(), User.GetSessionId(),
            Guid.Parse(User.FindFirst("sst")!.Value), conversationId, limit, before, after, through), cancellationToken));
    }
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

    [HttpPost("{conversationId:guid}/messages")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageResponse>> Send(Guid conversationId, SendMessageRequest request,
        CancellationToken cancellationToken) => FromResult(await sender.SendAsync(new(
            User.GetUserId(), User.GetSessionId(), Guid.Parse(User.FindFirst("sst")!.Value), conversationId,
            request.ClientMessageId!.Value, request.Content), cancellationToken));

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

public sealed record SendMessageRequest([Required] Guid? ClientMessageId,
    [property: System.Text.Json.Serialization.JsonConverter(typeof(DmContentConverter))] string? Content);

public sealed class DmContentConverter : System.Text.Json.Serialization.JsonConverter<string>
{
    public override string? Read(ref System.Text.Json.Utf8JsonReader reader, Type type, System.Text.Json.JsonSerializerOptions options)
    {
        if (reader.TokenType != System.Text.Json.JsonTokenType.String) throw new System.Text.Json.JsonException();
        try { return reader.GetString(); }
        catch (InvalidOperationException) { return "\ud800"; }
    }
    public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options) => writer.WriteStringValue(value);
}
