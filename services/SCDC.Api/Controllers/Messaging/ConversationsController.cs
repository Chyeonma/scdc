using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Controllers.Messaging;

[Authorize]
[Route("api/v1/conversations")]
public sealed class ConversationsController(
    IDirectConversationService directConversationService,
    IGroupConversationService groupConversationService) : ApiControllerBase
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

    [HttpGet("group")]
    [ProducesResponseType<IReadOnlyList<GroupConversationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupConversationDto>>> ListGroups(CancellationToken cancellationToken) =>
        FromResult(await groupConversationService.ListAsync(User.GetUserId(), cancellationToken));

    [HttpPost("group")]
    [ProducesResponseType<GroupConversationDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<GroupConversationDto>> CreateGroup(
        CreateGroupConversationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await groupConversationService.CreateAsync(new CreateGroupConversationCommand(
            User.GetUserId(), request.Name, request.MemberUserIds, request.MaxMembers, request.AvatarObjectKey), cancellationToken);
        return result.IsSuccess
            ? Created($"/api/v1/conversations/group/{result.Value.SpaceId}", result.Value)
            : FromResult(result);
    }

    [HttpGet("group/{spaceId:guid}")]
    [ProducesResponseType<GroupConversationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GroupConversationDto>> GetGroup(Guid spaceId, CancellationToken cancellationToken) =>
        FromResult(await groupConversationService.GetAsync(User.GetUserId(), spaceId, cancellationToken));

    [HttpPatch("group/{spaceId:guid}")]
    [ProducesResponseType<GroupConversationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GroupConversationDto>> UpdateGroup(
        Guid spaceId,
        UpdateGroupConversationRequest request,
        CancellationToken cancellationToken) =>
        FromResult(await groupConversationService.UpdateAsync(new UpdateGroupConversationCommand(
            User.GetUserId(), spaceId, request.Name, request.AvatarObjectKey, request.MaxMembers), cancellationToken));

    [HttpGet("group/{spaceId:guid}/members")]
    [ProducesResponseType<IReadOnlyList<GroupMemberDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupMemberDto>>> ListGroupMembers(Guid spaceId, CancellationToken cancellationToken) =>
        FromResult(await groupConversationService.ListMembersAsync(User.GetUserId(), spaceId, cancellationToken));

    [HttpPost("group/{spaceId:guid}/members")]
    public async Task<IActionResult> AddGroupMember(Guid spaceId, GroupMemberRequest request, CancellationToken cancellationToken) =>
        FromNoContentResult(await groupConversationService.AddMemberAsync(new ChangeGroupMemberCommand(User.GetUserId(), spaceId, request.UserId), cancellationToken));

    [HttpDelete("group/{spaceId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveGroupMember(Guid spaceId, Guid userId, CancellationToken cancellationToken) =>
        FromNoContentResult(await groupConversationService.RemoveMemberAsync(new ChangeGroupMemberCommand(User.GetUserId(), spaceId, userId), cancellationToken));

    [HttpPut("group/{spaceId:guid}/members/{userId:guid}/role")]
    public async Task<IActionResult> ChangeGroupMemberRole(
        Guid spaceId,
        Guid userId,
        ChangeGroupMemberRoleRequest request,
        CancellationToken cancellationToken) =>
        FromNoContentResult(await groupConversationService.ChangeMemberRoleAsync(
            new ChangeGroupMemberRoleCommand(User.GetUserId(), spaceId, userId, request.Role), cancellationToken));

    [HttpPost("group/{spaceId:guid}/leave")]
    public async Task<IActionResult> LeaveGroup(Guid spaceId, CancellationToken cancellationToken) =>
        FromNoContentResult(await groupConversationService.LeaveAsync(new ChangeGroupMemberCommand(User.GetUserId(), spaceId, User.GetUserId()), cancellationToken));

    [HttpPut("group/{spaceId:guid}/owner")]
    public async Task<IActionResult> ChangeGroupOwner(Guid spaceId, GroupMemberRequest request, CancellationToken cancellationToken) =>
        FromNoContentResult(await groupConversationService.ChangeOwnerAsync(new ChangeGroupOwnerCommand(User.GetUserId(), spaceId, request.UserId), cancellationToken));
}

public sealed record CreateDirectConversationRequest(
    [param: Required] Guid RecipientUserId);

public sealed record CreateGroupConversationRequest(
    [param: Required, StringLength(100, MinimumLength = 1)] string Name,
    [param: Required, MinLength(2)] IReadOnlyCollection<Guid> MemberUserIds,
    [param: Range(3, int.MaxValue)] int? MaxMembers,
    [param: StringLength(500)] string? AvatarObjectKey);

public sealed record UpdateGroupConversationRequest(
    [param: Required, StringLength(100, MinimumLength = 1)] string Name,
    [param: StringLength(500)] string? AvatarObjectKey,
    [param: Range(3, int.MaxValue)] int? MaxMembers);

public sealed record GroupMemberRequest([param: Required] Guid UserId);
public sealed record ChangeGroupMemberRoleRequest([param: Range(1, 2)] short Role);
