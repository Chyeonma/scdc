using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Messaging.Application;

public interface IGroupConversationService
{
    Task<Result<GroupConversationDto>> CreateAsync(CreateGroupConversationCommand command, CancellationToken cancellationToken);
    Task<Result<GroupConversationDto>> GetAsync(Guid actorUserId, Guid spaceId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<GroupConversationDto>>> ListAsync(Guid actorUserId, bool includeHidden, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<GroupMemberDto>>> ListMembersAsync(Guid actorUserId, Guid spaceId, CancellationToken cancellationToken);
    Task<Result<GroupConversationDto>> UpdateAsync(UpdateGroupConversationCommand command, CancellationToken cancellationToken);
    Task<Result> AddMemberAsync(ChangeGroupMemberCommand command, CancellationToken cancellationToken);
    Task<Result> RemoveMemberAsync(ChangeGroupMemberCommand command, CancellationToken cancellationToken);
    Task<Result> LeaveAsync(ChangeGroupMemberCommand command, CancellationToken cancellationToken);
    Task<Result> ChangeMemberRoleAsync(ChangeGroupMemberRoleCommand command, CancellationToken cancellationToken);
    Task<Result> ChangeOwnerAsync(ChangeGroupOwnerCommand command, CancellationToken cancellationToken);
}
