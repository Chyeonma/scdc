using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Realtime;

namespace SCDC.Api.Controllers.Messaging;

[Authorize]
[Route("api/v1/group-conversations/{conversationId:guid}/messages/{messageId:guid}/like")]
public sealed class GroupMessageLikesController(IGroupMessageLikes likes, GroupChatEvents events) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MessageLikes>> Read(Guid conversationId, Guid messageId, CancellationToken token) =>
        FromResult(await likes.ReadAsync(RequestFor(conversationId, messageId), token));

    [HttpPut]
    public Task<ActionResult<MessageLikes>> Like(Guid conversationId, Guid messageId, CancellationToken token) =>
        SetAsync(conversationId, messageId, true, token);

    [HttpDelete]
    public Task<ActionResult<MessageLikes>> Unlike(Guid conversationId, Guid messageId, CancellationToken token) =>
        SetAsync(conversationId, messageId, false, token);

    private async Task<ActionResult<MessageLikes>> SetAsync(Guid conversationId, Guid messageId, bool liked, CancellationToken token)
    {
        var result = await likes.SetAsync(RequestFor(conversationId, messageId), liked, token);
        if (result.IsSuccess) await events.LikesChangedAsync(conversationId, messageId, token);
        return FromResult(result);
    }

    private GroupMessageLikeRequest RequestFor(Guid conversationId, Guid messageId) =>
        new(new(User.GetUserId(), User.GetSessionId(), Guid.Parse(User.FindFirst("sst")!.Value), User.GetAccessExpiresAt()),
            conversationId, messageId);
}
