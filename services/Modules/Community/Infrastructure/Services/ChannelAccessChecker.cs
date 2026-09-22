using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Community;
using SCDC.Modules.Community.Domain;
using SCDC.Modules.Community.Infrastructure.Persistence;

namespace SCDC.Modules.Community.Infrastructure.Services;

internal sealed class ChannelAccessChecker(CommunityDbContext dbContext) : IChannelAccessChecker
{
    public async Task<ChannelAccessDecision> CheckAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || spaceId == Guid.Empty) return ChannelAccessDecision.Denied;
        var channel = await dbContext.Channels.AsNoTracking().SingleOrDefaultAsync(x => x.SpaceId == spaceId, cancellationToken);
        if (channel is null) return ChannelAccessDecision.Denied;
        var member = await dbContext.Members.AsNoTracking().SingleOrDefaultAsync(x => x.ServerId == channel.ServerId && x.UserId == userId, cancellationToken);
        if (member?.Status != MemberStatus.Active) return ChannelAccessDecision.Denied;
        var server = await dbContext.Servers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == channel.ServerId && x.Status == ServerStatus.Active, cancellationToken);
        if (server is null) return ChannelAccessDecision.Denied;
        if (server.OwnerUserId == userId) return new(true, true);

        var roleIds = await dbContext.MemberRoles.AsNoTracking().Where(x => x.ServerId == channel.ServerId && x.UserId == userId).Select(x => x.RoleId).ToListAsync(cancellationToken);
        var defaultRoles = await dbContext.Roles.AsNoTracking().Where(x => x.ServerId == channel.ServerId && x.IsDefault).Select(x => x.Id).ToListAsync(cancellationToken);
        roleIds.AddRange(defaultRoles);
        var permissions = await dbContext.RolePermissions.AsNoTracking().Where(x => roleIds.Contains(x.RoleId)).Select(x => x.PermissionCode).ToListAsync(cancellationToken);
        var canManage = permissions.Contains("server.manage") || permissions.Contains("channel.manage");
        var canRead = channel.Visibility != ChannelVisibility.Private || canManage || permissions.Contains("channel.read");
        var canSend = channel.Visibility != ChannelVisibility.ReadOnly && canRead;
        if (permissions.Contains("channel.send")) canSend = canRead;

        var roleOverrides = await dbContext.ChannelRoleOverrides.AsNoTracking().Where(x => x.SpaceId == spaceId && roleIds.Contains(x.RoleId) && (x.PermissionCode == "channel.read" || x.PermissionCode == "channel.send")).ToListAsync(cancellationToken);
        var userOverrides = await dbContext.ChannelUserOverrides.AsNoTracking().Where(x => x.SpaceId == spaceId && x.UserId == userId && (x.PermissionCode == "channel.read" || x.PermissionCode == "channel.send")).ToListAsync(cancellationToken);
        canRead = Apply("channel.read", canRead, roleOverrides, userOverrides);
        canSend = canRead && (canManage || Apply("channel.send", canSend, roleOverrides, userOverrides));
        return new(canRead, canSend);
    }

    public async Task<IReadOnlyList<Guid>> ListReadableMemberIdsAsync(Guid spaceId, CancellationToken cancellationToken)
    {
        var channel = await dbContext.Channels.AsNoTracking().SingleOrDefaultAsync(x => x.SpaceId == spaceId, cancellationToken);
        if (channel is null) return [];
        var ids = await dbContext.Members.AsNoTracking().Where(x => x.ServerId == channel.ServerId && x.Status == MemberStatus.Active).Select(x => x.UserId).ToArrayAsync(cancellationToken);
        var result = new List<Guid>();
        foreach (var id in ids) if ((await CheckAsync(id, spaceId, cancellationToken)).CanRead) result.Add(id);
        return result;
    }

    private static bool Apply(string code, bool value, IReadOnlyCollection<ChannelRoleOverride> roles, IReadOnlyCollection<ChannelUserOverride> users)
    {
        var user = users.SingleOrDefault(x => x.PermissionCode == code);
        if (user is not null) return user.Effect == PermissionEffect.Allow;
        var matching = roles.Where(x => x.PermissionCode == code).ToArray();
        if (matching.Any(x => x.Effect == PermissionEffect.Allow)) return true;
        if (matching.Any(x => x.Effect == PermissionEffect.Deny)) return false;
        return value;
    }
}
