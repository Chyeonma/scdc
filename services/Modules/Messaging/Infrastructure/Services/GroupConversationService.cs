using Microsoft.EntityFrameworkCore;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class GroupConversationService(
    MessagingDbContext dbContext,
    IUserDirectory userDirectory,
    IRealtimeAccessRevoker realtimeAccessRevoker,
    MessagingRealtimeAccessRevoker realtimeEvents,
    TimeProvider timeProvider) : IGroupConversationService
{
    public async Task<Result<GroupConversationDto>> CreateAsync(
        CreateGroupConversationCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeName(command.Name, out var name)
            || command.ActorUserId == Guid.Empty
            || command.MaxMembers is < 3)
        {
            return Result.Failure<GroupConversationDto>(MessagingErrors.InvalidGroup);
        }

        var requestedMemberIds = (command.MemberUserIds ?? []).ToArray();
        if (requestedMemberIds.Any(id => id == Guid.Empty || id == command.ActorUserId)
            || requestedMemberIds.Distinct().Count() != requestedMemberIds.Length)
        {
            return Result.Failure<GroupConversationDto>(MessagingErrors.InvalidGroup);
        }
        var memberIds = requestedMemberIds;
        if (memberIds.Length < 2 || (command.MaxMembers is { } max && memberIds.Length + 1 > max))
        {
            return Result.Failure<GroupConversationDto>(MessagingErrors.InvalidGroup);
        }

        var allUserIds = memberIds.Append(command.ActorUserId).ToArray();
        var users = await userDirectory.FindByIdsAsync(allUserIds, cancellationToken);
        if (users.Count != allUserIds.Length)
        {
            return Result.Failure<GroupConversationDto>(MessagingErrors.GroupMemberUnavailable);
        }

        var blocked = await dbContext.UserBlocks.AsNoTracking().AnyAsync(block =>
            memberIds.Contains(block.BlockerUserId) && block.BlockedUserId == command.ActorUserId
            || memberIds.Contains(block.BlockedUserId) && block.BlockerUserId == command.ActorUserId,
            cancellationToken);
        if (blocked)
        {
            return Result.Failure<GroupConversationDto>(MessagingErrors.GroupMemberUnavailable);
        }

        var now = timeProvider.GetUtcNow();
        var spaceId = Guid.CreateVersion7();
        var space = new ChatSpace
        {
            Id = spaceId,
            SpaceType = SpaceType.Group,
            Status = SpaceStatus.Active,
            CreatedByUserId = command.ActorUserId,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1,
        };
        var group = new GroupConversation
        {
            SpaceId = spaceId,
            Name = name,
            AvatarObjectKey = NormalizeAvatar(command.AvatarObjectKey),
            OwnerUserId = command.ActorUserId,
            MaxMembers = command.MaxMembers,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Spaces.Add(space);
        dbContext.GroupConversations.Add(group);
        dbContext.SpaceMembers.AddRange(allUserIds.Select(userId => new SpaceMember
        {
            SpaceId = spaceId,
            UserId = userId,
            MemberRole = userId == command.ActorUserId ? SpaceMemberRole.Owner : SpaceMemberRole.Member,
            MembershipStatus = SpaceMembershipStatus.Active,
            JoinedAt = now,
        }));
        dbContext.SpaceUserStates.AddRange(allUserIds.Select(userId => new SpaceUserState
        {
            SpaceId = spaceId,
            UserId = userId,
            NotificationLevel = NotificationLevel.AllMessages,
            UpdatedAt = now,
        }));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await realtimeEvents.NotifySpaceUpdatedAsync(allUserIds, spaceId, cancellationToken);

        return Result.Success(ToDto(space, group, allUserIds.Length));
    }

    public async Task<Result<GroupConversationDto>> GetAsync(Guid actorUserId, Guid spaceId, CancellationToken cancellationToken)
    {
        var group = await FindVisibleGroupAsync(actorUserId, spaceId, cancellationToken);
        if (group is null) return Result.Failure<GroupConversationDto>(MessagingErrors.ResourceNotFound);
        var count = await ActiveMemberCountAsync(spaceId, cancellationToken);
        return Result.Success(ToDto(group.Space, group.Group, count));
    }

    public async Task<Result<IReadOnlyList<GroupConversationDto>>> ListAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty) return Result.Failure<IReadOnlyList<GroupConversationDto>>(MessagingErrors.AccountUnavailable);
        var rows = await (
            from conversation in dbContext.GroupConversations.AsNoTracking()
            join space in dbContext.Spaces.AsNoTracking() on conversation.SpaceId equals space.Id
            join member in dbContext.SpaceMembers.AsNoTracking() on space.Id equals member.SpaceId
            where member.UserId == actorUserId
                  && member.MembershipStatus == SpaceMembershipStatus.Active
                  && space.Status != SpaceStatus.Deleted
            orderby space.LastActivityAt descending, space.Id descending
            select new { Space = space, Group = conversation })
            .ToListAsync(cancellationToken);
        var ids = rows.Select(row => row.Space.Id).ToArray();
        var counts = await dbContext.SpaceMembers.AsNoTracking()
            .Where(member => ids.Contains(member.SpaceId) && member.MembershipStatus == SpaceMembershipStatus.Active)
            .GroupBy(member => member.SpaceId)
            .Select(memberSet => new { SpaceId = memberSet.Key, Count = memberSet.Count() })
            .ToDictionaryAsync(item => item.SpaceId, item => item.Count, cancellationToken);
        return Result.Success<IReadOnlyList<GroupConversationDto>>(rows
            .Select(row => ToDto(row.Space, row.Group, counts.GetValueOrDefault(row.Space.Id)))
            .ToArray());
    }

    public async Task<Result<IReadOnlyList<GroupMemberDto>>> ListMembersAsync(Guid actorUserId, Guid spaceId, CancellationToken cancellationToken)
    {
        if (await FindVisibleGroupAsync(actorUserId, spaceId, cancellationToken) is null)
            return Result.Failure<IReadOnlyList<GroupMemberDto>>(MessagingErrors.ResourceNotFound);
        var members = await dbContext.SpaceMembers.AsNoTracking()
            .Where(member => member.SpaceId == spaceId && member.MembershipStatus == SpaceMembershipStatus.Active)
            .OrderByDescending(member => member.MemberRole).ThenBy(member => member.JoinedAt)
            .ToListAsync(cancellationToken);
        var users = await userDirectory.FindByIdsAsync(members.Select(member => member.UserId).ToArray(), cancellationToken);
        return Result.Success<IReadOnlyList<GroupMemberDto>>(members
            .Where(member => users.ContainsKey(member.UserId))
            .Select(member => new GroupMemberDto(users[member.UserId], (short)member.MemberRole, member.JoinedAt))
            .ToArray());
    }

    public async Task<Result<GroupConversationDto>> UpdateAsync(UpdateGroupConversationCommand command, CancellationToken cancellationToken)
    {
        if (command.SpaceId == Guid.Empty || command.ActorUserId == Guid.Empty
            || !TryNormalizeName(command.Name, out var name) || command.MaxMembers is < 3)
            return Result.Failure<GroupConversationDto>(MessagingErrors.InvalidGroup);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var locked = await FindLockedAsync(command.SpaceId, command.ActorUserId, cancellationToken);
        if (locked is null || !CanManage(locked.ActorMember, includeOwner: true))
            return Result.Failure<GroupConversationDto>(MessagingErrors.ResourceNotFound);
        var count = await ActiveMemberCountAsync(command.SpaceId, cancellationToken);
        if (command.MaxMembers is { } max && max < count)
            return Result.Failure<GroupConversationDto>(MessagingErrors.GroupMemberLimitReached);

        locked.Group.Name = name;
        locked.Group.AvatarObjectKey = NormalizeAvatar(command.AvatarObjectKey);
        locked.Group.MaxMembers = command.MaxMembers;
        locked.Group.UpdatedAt = timeProvider.GetUtcNow();
        Touch(locked.Space);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await NotifyMembersAsync(command.SpaceId, cancellationToken);
        return Result.Success(ToDto(locked.Space, locked.Group, count));
    }

    public async Task<Result> AddMemberAsync(ChangeGroupMemberCommand command, CancellationToken cancellationToken)
    {
        if (command.MemberUserId == Guid.Empty || command.ActorUserId == Guid.Empty || command.SpaceId == Guid.Empty)
            return Result.Failure(MessagingErrors.InvalidGroup);
        if (await userDirectory.FindByIdAsync(command.MemberUserId, cancellationToken) is null)
            return Result.Failure(MessagingErrors.GroupMemberUnavailable);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var locked = await FindLockedAsync(command.SpaceId, command.ActorUserId, cancellationToken);
        if (locked is null || !CanManage(locked.ActorMember, includeOwner: true)) return Result.Failure(MessagingErrors.ResourceNotFound);
        var isBlocked = await dbContext.UserBlocks.AsNoTracking().AnyAsync(block =>
            (block.BlockerUserId == command.ActorUserId && block.BlockedUserId == command.MemberUserId)
            || (block.BlockerUserId == command.MemberUserId && block.BlockedUserId == command.ActorUserId), cancellationToken);
        if (isBlocked) return Result.Failure(MessagingErrors.GroupMemberUnavailable);

        var count = await ActiveMemberCountAsync(command.SpaceId, cancellationToken);
        var member = await dbContext.SpaceMembers.SingleOrDefaultAsync(item => item.SpaceId == command.SpaceId && item.UserId == command.MemberUserId, cancellationToken);
        if (member?.MembershipStatus == SpaceMembershipStatus.Active) return Result.Success();
        if (locked.Group.MaxMembers is { } max && count >= max) return Result.Failure(MessagingErrors.GroupMemberLimitReached);
        var now = timeProvider.GetUtcNow();
        if (member is null)
        {
            dbContext.SpaceMembers.Add(new SpaceMember { SpaceId = command.SpaceId, UserId = command.MemberUserId, MemberRole = SpaceMemberRole.Member, MembershipStatus = SpaceMembershipStatus.Active, JoinedAt = now });
            dbContext.SpaceUserStates.Add(new SpaceUserState { SpaceId = command.SpaceId, UserId = command.MemberUserId, NotificationLevel = NotificationLevel.AllMessages, UpdatedAt = now });
        }
        else
        {
            member.MemberRole = SpaceMemberRole.Member;
            member.MembershipStatus = SpaceMembershipStatus.Active;
            member.JoinedAt = now;
            member.LeftAt = null;
            member.RemovedByUserId = null;
        }
        Touch(locked.Space);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await realtimeEvents.NotifySpaceUpdatedAsync([command.MemberUserId], command.SpaceId, cancellationToken);
        return Result.Success();
    }

    public Task<Result> RemoveMemberAsync(ChangeGroupMemberCommand command, CancellationToken cancellationToken) =>
        ExitAsync(command, removed: true, cancellationToken);

    public Task<Result> LeaveAsync(ChangeGroupMemberCommand command, CancellationToken cancellationToken) =>
        ExitAsync(command with { MemberUserId = command.ActorUserId }, removed: false, cancellationToken);

    public async Task<Result> ChangeMemberRoleAsync(ChangeGroupMemberRoleCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorUserId == Guid.Empty || command.SpaceId == Guid.Empty || command.MemberUserId == Guid.Empty
            || command.Role is not ((short)SpaceMemberRole.Member or (short)SpaceMemberRole.Admin))
            return Result.Failure(MessagingErrors.InvalidGroup);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var locked = await FindLockedAsync(command.SpaceId, command.ActorUserId, cancellationToken);
        if (locked is null || locked.ActorMember?.MemberRole != SpaceMemberRole.Owner)
            return Result.Failure(MessagingErrors.ResourceNotFound);
        var target = await dbContext.SpaceMembers.SingleOrDefaultAsync(member =>
            member.SpaceId == command.SpaceId && member.UserId == command.MemberUserId && member.MembershipStatus == SpaceMembershipStatus.Active,
            cancellationToken);
        if (target is null || target.MemberRole == SpaceMemberRole.Owner) return Result.Failure(MessagingErrors.ActionNotAllowed);
        target.MemberRole = (SpaceMemberRole)command.Role;
        Touch(locked.Space);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await NotifyMembersAsync(command.SpaceId, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ChangeOwnerAsync(ChangeGroupOwnerCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorUserId == Guid.Empty || command.SpaceId == Guid.Empty || command.NewOwnerUserId == Guid.Empty)
            return Result.Failure(MessagingErrors.InvalidGroup);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var locked = await FindLockedAsync(command.SpaceId, command.ActorUserId, cancellationToken);
        if (locked is null || locked.ActorMember?.MemberRole != SpaceMemberRole.Owner) return Result.Failure(MessagingErrors.ResourceNotFound);
        var successor = await dbContext.SpaceMembers.SingleOrDefaultAsync(member => member.SpaceId == command.SpaceId && member.UserId == command.NewOwnerUserId && member.MembershipStatus == SpaceMembershipStatus.Active, cancellationToken);
        if (successor is null) return Result.Failure(MessagingErrors.ResourceNotFound);
        if (successor.UserId == command.ActorUserId) return Result.Success();
        locked.ActorMember.MemberRole = SpaceMemberRole.Admin;
        successor.MemberRole = SpaceMemberRole.Owner;
        locked.Group.OwnerUserId = successor.UserId;
        locked.Group.UpdatedAt = timeProvider.GetUtcNow();
        Touch(locked.Space);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await NotifyMembersAsync(command.SpaceId, cancellationToken);
        return Result.Success();
    }

    private async Task<Result> ExitAsync(ChangeGroupMemberCommand command, bool removed, CancellationToken cancellationToken)
    {
        if (command.ActorUserId == Guid.Empty || command.SpaceId == Guid.Empty || command.MemberUserId == Guid.Empty)
            return Result.Failure(MessagingErrors.InvalidGroup);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var locked = await FindLockedAsync(command.SpaceId, command.ActorUserId, cancellationToken);
        if (locked is null || locked.ActorMember is null) return Result.Failure(MessagingErrors.ResourceNotFound);
        if (removed && !CanManage(locked.ActorMember, includeOwner: true)) return Result.Failure(MessagingErrors.ResourceNotFound);
        var target = await dbContext.SpaceMembers.SingleOrDefaultAsync(member => member.SpaceId == command.SpaceId && member.UserId == command.MemberUserId && member.MembershipStatus == SpaceMembershipStatus.Active, cancellationToken);
        if (target is null) return Result.Success();
        if (target.MemberRole == SpaceMemberRole.Owner) return Result.Failure(MessagingErrors.GroupOwnerTransferRequired);
        if (removed && locked.ActorMember.MemberRole == SpaceMemberRole.Admin && target.MemberRole != SpaceMemberRole.Member)
            return Result.Failure(MessagingErrors.ActionNotAllowed);

        target.MembershipStatus = removed ? SpaceMembershipStatus.Removed : SpaceMembershipStatus.Left;
        target.LeftAt = timeProvider.GetUtcNow();
        target.RemovedByUserId = removed ? command.ActorUserId : null;
        Touch(locked.Space);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await realtimeAccessRevoker.RevokeAsync(command.MemberUserId, [command.SpaceId], cancellationToken);
        return Result.Success();
    }

    private async Task<GroupRow?> FindVisibleGroupAsync(Guid actorUserId, Guid spaceId, CancellationToken cancellationToken) => await (
        from conversation in dbContext.GroupConversations.AsNoTracking()
        join space in dbContext.Spaces.AsNoTracking() on conversation.SpaceId equals space.Id
        join member in dbContext.SpaceMembers.AsNoTracking() on space.Id equals member.SpaceId
        where space.Id == spaceId && member.UserId == actorUserId && member.MembershipStatus == SpaceMembershipStatus.Active && space.Status != SpaceStatus.Deleted
        select new GroupRow(space, conversation, member))
        .SingleOrDefaultAsync(cancellationToken);

    private async Task<LockedGroup?> FindLockedAsync(Guid spaceId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var space = await dbContext.Spaces.FromSqlInterpolated($"SELECT * FROM messaging.spaces WHERE id = {spaceId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);
        if (space is null || space.SpaceType != SpaceType.Group || space.Status == SpaceStatus.Deleted) return null;
        var group = await dbContext.GroupConversations.SingleOrDefaultAsync(item => item.SpaceId == spaceId, cancellationToken);
        if (group is null) return null;
        var actorMember = await dbContext.SpaceMembers.SingleOrDefaultAsync(member =>
            member.SpaceId == spaceId && member.UserId == actorUserId && member.MembershipStatus == SpaceMembershipStatus.Active,
            cancellationToken);
        return new LockedGroup(space, group, actorMember);
    }

    private static bool CanManage(SpaceMember? member, bool includeOwner) => member is not null && (member.MemberRole == SpaceMemberRole.Admin || includeOwner && member.MemberRole == SpaceMemberRole.Owner);
    private async Task NotifyMembersAsync(Guid spaceId, CancellationToken cancellationToken) =>
        await realtimeEvents.NotifySpaceUpdatedAsync(await dbContext.SpaceMembers.AsNoTracking()
            .Where(member => member.SpaceId == spaceId && member.MembershipStatus == SpaceMembershipStatus.Active)
            .Select(member => member.UserId).ToArrayAsync(cancellationToken), spaceId, cancellationToken);
    private async Task<int> ActiveMemberCountAsync(Guid spaceId, CancellationToken cancellationToken) => await dbContext.SpaceMembers.CountAsync(member => member.SpaceId == spaceId && member.MembershipStatus == SpaceMembershipStatus.Active, cancellationToken);
    private static void Touch(ChatSpace space) { space.Version++; space.UpdatedAt = DateTimeOffset.UtcNow; }
    private static bool TryNormalizeName(string? value, out string name) { name = value?.Trim() ?? string.Empty; return name.Length is >= 1 and <= 100; }
    private static string? NormalizeAvatar(string? value) { var avatar = value?.Trim(); return string.IsNullOrEmpty(avatar) ? null : avatar.Length <= 500 ? avatar : null; }
    private static GroupConversationDto ToDto(ChatSpace space, GroupConversation group, int count) => new(space.Id, group.Name, group.AvatarObjectKey, group.OwnerUserId, group.MaxMembers, count, (short)space.Status, space.Version, space.LastActivityAt);
    private sealed record GroupRow(ChatSpace Space, GroupConversation Group, SpaceMember Member);
    private sealed record LockedGroup(ChatSpace Space, GroupConversation Group, SpaceMember? ActorMember);
}
