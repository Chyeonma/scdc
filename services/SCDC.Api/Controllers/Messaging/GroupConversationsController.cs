using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Application.GroupConversations;
using SCDC.Modules.Messaging.Domain;

namespace SCDC.Api.Controllers.Messaging;

[Authorize]
[Route("api/v1/group-conversations")]
public sealed class GroupConversationsController(CreateGroupHandler create, ListGroupsHandler list,
    ITextMessageSender sender, IMessageHistory history, IMessageMutator mutator)
    : ConversationMessagesController(sender, history, mutator)
{
    protected override ConversationKind Kind => ConversationKind.Group;

    [HttpPost]
    public async Task<ActionResult<GroupSummary>> Create(CreateGroupRequest request, CancellationToken token) =>
        FromResult(await create.HandleAsync(new(Actor(), request.Id, request.Name, request.MemberIds), token));

    [HttpGet]
    public async Task<ActionResult<GroupPage>> List([FromQuery, Range(1, 50)] int limit = 20, [FromQuery] Guid? cursor = null,
        CancellationToken token = default) =>
        FromResult(await list.HandleAsync(new(Actor(), limit, cursor), token));

    private AuthenticatedSession Actor() => new(User.GetUserId(), User.GetSessionId(),
        Guid.Parse(User.FindFirst("sst")!.Value), User.GetAccessExpiresAt());
}

public sealed record CreateGroupRequest(Guid Id, string? Name, IReadOnlyList<Guid>? MemberIds);
