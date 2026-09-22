using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Community.Application;
using SCDC.Modules.Community.Domain;
using SCDC.Modules.Community.Infrastructure.Persistence;

namespace SCDC.Modules.Community.Infrastructure.Services;

internal sealed class CommunityService(
    CommunityDbContext db,
    IUserDirectory users,
    IChannelSpaceProvisioner spaces,
    IRealtimeAccessRevoker revoker,
    TimeProvider clock) : ICommunityService
{
    private static readonly string[] OwnerPermissions = ["channel.read", "channel.send", "channel.manage", "server.manage", "member.manage", "invite.create"];

    public async Task<Result<IReadOnlyList<ServerDto>>> ListServersAsync(Guid actor, CancellationToken ct)
    {
        var rows = await (from s in db.Servers.AsNoTracking()
                          join m in db.Members.AsNoTracking() on s.Id equals m.ServerId
                          where m.UserId == actor && m.Status == MemberStatus.Active && s.Status == ServerStatus.Active
                          orderby s.Name
                          select s).ToListAsync(ct);
        return Result.Success<IReadOnlyList<ServerDto>>(rows.Select(ToDto).ToArray());
    }

    public async Task<Result<ServerDto>> CreateServerAsync(CreateServerCommand command, CancellationToken ct)
    {
        if (command.ActorUserId == Guid.Empty || !TryName(command.Name, 2, 100, out var name)) return Result.Failure<ServerDto>(CommunityErrors.Invalid);
        if (await users.FindByIdAsync(command.ActorUserId, ct) is null) return Result.Failure<ServerDto>(CommunityErrors.Forbidden);
        var now = clock.GetUtcNow();
        var id = Guid.CreateVersion7();
        var slug = await NextSlugAsync(name, ct);
        var ownerRole = new Role { Id = Guid.CreateVersion7(), ServerId = id, Name = "Owner", Position = 100, IsSystem = true };
        var everyoneRole = new Role { Id = Guid.CreateVersion7(), ServerId = id, Name = "@everyone", Position = 0, IsDefault = true, IsSystem = true };
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        foreach (var code in OwnerPermissions)
            if (!await db.Permissions.AnyAsync(x => x.Code == code, ct)) db.Permissions.Add(new Permission { Code = code, Description = code });
        await db.SaveChangesAsync(ct);
        db.Servers.Add(new Server { Id = id, OwnerUserId = command.ActorUserId, Name = name, Slug = slug, Description = Normalize(command.Description, 500), Status = ServerStatus.Active, CreatedAt = now, UpdatedAt = now, Version = 1 });
        await db.SaveChangesAsync(ct);
        db.Members.Add(new ServerMember { ServerId = id, UserId = command.ActorUserId, Status = MemberStatus.Active, JoinedAt = now });
        await db.SaveChangesAsync(ct);
        db.Roles.AddRange(ownerRole, everyoneRole);
        await db.SaveChangesAsync(ct);
        db.MemberRoles.Add(new MemberRole { ServerId = id, UserId = command.ActorUserId, RoleId = ownerRole.Id });
        db.RolePermissions.AddRange(OwnerPermissions.Select(code => new RolePermission { RoleId = ownerRole.Id, PermissionCode = code }));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Result.Success(new ServerDto(id, name, slug, Normalize(command.Description, 500), command.ActorUserId, (short)ServerStatus.Active));
    }

    public async Task<Result<IReadOnlyList<ChannelDto>>> ListChannelsAsync(Guid actor, Guid serverId, CancellationToken ct)
    {
        if (!await IsActiveMemberAsync(actor, serverId, ct)) return Result.Failure<IReadOnlyList<ChannelDto>>(CommunityErrors.NotFound);
        var channels = await db.Channels.AsNoTracking().Where(x => x.ServerId == serverId).OrderBy(x => x.Position).ThenBy(x => x.Name).ToListAsync(ct);
        var checker = new ChannelAccessChecker(db);
        var items = new List<ChannelDto>();
        foreach (var channel in channels)
        {
            var access = await checker.CheckAsync(actor, channel.SpaceId, ct);
            if (access.CanRead) items.Add(ToDto(channel, access));
        }
        return Result.Success<IReadOnlyList<ChannelDto>>(items);
    }

    public async Task<Result<ChannelDto>> CreateChannelAsync(CreateChannelCommand command, CancellationToken ct)
    {
        if (!TryChannelName(command.Name, out var name) || command.Visibility is < 1 or > 3) return Result.Failure<ChannelDto>(CommunityErrors.Invalid);
        if (!await CanManageAsync(command.ActorUserId, command.ServerId, "channel.manage", ct)) return Result.Failure<ChannelDto>(CommunityErrors.Forbidden);
        if (await db.Channels.AnyAsync(x => x.ServerId == command.ServerId && x.Name == name, ct)) return Result.Failure<ChannelDto>(CommunityErrors.Conflict);
        var provision = await spaces.CreateAsync(command.ActorUserId, ct);
        if (!provision.IsSuccess) return Result.Failure<ChannelDto>(CommunityErrors.ProvisioningFailed);
        try
        {
            var position = (await db.Channels.Where(x => x.ServerId == command.ServerId).Select(x => (int?)x.Position).MaxAsync(ct) ?? -1) + 1;
            var now = clock.GetUtcNow();
            var channel = new Channel { SpaceId = provision.SpaceId!.Value, ServerId = command.ServerId, Name = name, Topic = Normalize(command.Topic, 500), Visibility = (ChannelVisibility)command.Visibility, Position = position, CreatedAt = now, UpdatedAt = now };
            db.Channels.Add(channel);
            await db.SaveChangesAsync(ct);
            var access = await new ChannelAccessChecker(db).CheckAsync(command.ActorUserId, channel.SpaceId, ct);
            return Result.Success(ToDto(channel, access));
        }
        catch (DbUpdateException)
        {
            await spaces.RetireAsync(provision.SpaceId!.Value, ct);
            return Result.Failure<ChannelDto>(CommunityErrors.Conflict);
        }
    }

    public async Task<Result<IReadOnlyList<CommunityMemberDto>>> ListMembersAsync(Guid actor, Guid serverId, CancellationToken ct)
    {
        if (!await IsActiveMemberAsync(actor, serverId, ct)) return Result.Failure<IReadOnlyList<CommunityMemberDto>>(CommunityErrors.NotFound);
        var members = await db.Members.AsNoTracking().Where(x => x.ServerId == serverId && x.Status == MemberStatus.Active).ToListAsync(ct);
        var map = await users.FindByIdsAsync(members.Select(x => x.UserId).ToArray(), ct);
        var roles = await (from r in db.Roles.AsNoTracking() join mr in db.MemberRoles.AsNoTracking() on r.Id equals mr.RoleId where mr.ServerId == serverId select new { mr.UserId, r.Name, r.Position }).ToListAsync(ct);
        return Result.Success<IReadOnlyList<CommunityMemberDto>>(members.Select(m => new CommunityMemberDto(m.UserId, map.GetValueOrDefault(m.UserId), m.Nickname, roles.Where(r => r.UserId == m.UserId).OrderByDescending(r => r.Position).Select(r => r.Name).FirstOrDefault() ?? "Member", (short)m.Status)).ToArray());
    }

    public async Task<Result<InviteDto>> CreateInviteAsync(CreateInviteCommand command, CancellationToken ct)
    {
        if (!await CanManageAsync(command.ActorUserId, command.ServerId, "invite.create", ct)) return Result.Failure<InviteDto>(CommunityErrors.Forbidden);
        if (command.MaxUses is <= 0 || command.ExpiresAt <= clock.GetUtcNow()) return Result.Failure<InviteDto>(CommunityErrors.Invalid);
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
        db.Invites.Add(new Invite { Id = Guid.CreateVersion7(), ServerId = command.ServerId, CodeHash = Hash(code), CreatedByUserId = command.ActorUserId, MaxUses = command.MaxUses, ExpiresAt = command.ExpiresAt, CreatedAt = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct);
        return Result.Success(new InviteDto(code, command.ExpiresAt, command.MaxUses));
    }

    public async Task<Result<ServerDto>> JoinInviteAsync(Guid actor, string code, CancellationToken ct)
    {
        if (actor == Guid.Empty || string.IsNullOrWhiteSpace(code)) return Result.Failure<ServerDto>(CommunityErrors.Invalid);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var invite = await db.Invites.FromSqlInterpolated($"SELECT * FROM community.invites WHERE code_hash = {Hash(code)} FOR UPDATE").SingleOrDefaultAsync(ct);
        var now = clock.GetUtcNow();
        if (invite is null || invite.RevokedAt is not null || invite.ExpiresAt <= now || (invite.MaxUses is not null && invite.UseCount >= invite.MaxUses)) return Result.Failure<ServerDto>(CommunityErrors.NotFound);
        var ban = await db.Bans.AsNoTracking().SingleOrDefaultAsync(x => x.ServerId == invite.ServerId && x.UserId == actor, ct);
        if (ban is { RevokedAt: null } && (ban.ExpiresAt is null || ban.ExpiresAt > now)) return Result.Failure<ServerDto>(CommunityErrors.Forbidden);
        var member = await db.Members.SingleOrDefaultAsync(x => x.ServerId == invite.ServerId && x.UserId == actor, ct);
        if (member is null) db.Members.Add(new ServerMember { ServerId = invite.ServerId, UserId = actor, Status = MemberStatus.Active, JoinedAt = now });
        else { member.Status = MemberStatus.Active; member.LeftAt = null; member.JoinedAt = now; }
        invite.UseCount++;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        var server = await db.Servers.AsNoTracking().SingleAsync(x => x.Id == invite.ServerId, ct);
        return Result.Success(ToDto(server));
    }

    public async Task<Result> LeaveAsync(Guid actor, Guid serverId, CancellationToken ct) => await RemoveAsync(actor, serverId, actor, MemberStatus.Left, false, null, ct);
    public async Task<Result> KickAsync(Guid actor, Guid serverId, Guid target, CancellationToken ct) => await RemoveAsync(actor, serverId, target, MemberStatus.Kicked, true, null, ct);
    public async Task<Result> BanAsync(Guid actor, Guid serverId, Guid target, string? reason, CancellationToken ct) => await RemoveAsync(actor, serverId, target, MemberStatus.Banned, true, reason, ct);

    public async Task<Result> SetRoleAsync(Guid actor, Guid serverId, Guid target, Guid roleId, bool assigned, CancellationToken ct)
    {
        if (!await CanManageAsync(actor, serverId, "server.manage", ct) || !await IsActiveMemberAsync(target, serverId, ct)) return Result.Failure(CommunityErrors.Forbidden);
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Id == roleId && x.ServerId == serverId && !x.IsSystem, ct);
        if (role is null) return Result.Failure(CommunityErrors.NotFound);
        var existing = await db.MemberRoles.SingleOrDefaultAsync(x => x.ServerId == serverId && x.UserId == target && x.RoleId == roleId, ct);
        if (assigned && existing is null) db.MemberRoles.Add(new MemberRole { ServerId = serverId, UserId = target, RoleId = roleId });
        if (!assigned && existing is not null) db.MemberRoles.Remove(existing);
        await db.SaveChangesAsync(ct); await RevokeServerAsync(target, serverId, ct); return Result.Success();
    }

    public async Task<Result<RoleDto>> CreateRoleAsync(CreateRoleCommand command, CancellationToken ct)
    {
        if (!TryName(command.Name, 1, 50, out var name) || !await CanManageAsync(command.ActorUserId, command.ServerId, "server.manage", ct)) return Result.Failure<RoleDto>(CommunityErrors.Forbidden);
        var codes = command.PermissionCodes.Distinct(StringComparer.Ordinal).ToArray();
        if (codes.Any(code => !OwnerPermissions.Contains(code, StringComparer.Ordinal)) || await db.Roles.AnyAsync(x => x.ServerId == command.ServerId && x.Name == name, ct)) return Result.Failure<RoleDto>(CommunityErrors.Conflict);
        var role = new Role { Id = Guid.CreateVersion7(), ServerId = command.ServerId, Name = name, Position = (await db.Roles.Where(x => x.ServerId == command.ServerId).Select(x => (int?)x.Position).MaxAsync(ct) ?? 0) + 1 };
        db.Roles.Add(role); await db.SaveChangesAsync(ct);
        db.RolePermissions.AddRange(codes.Select(code => new RolePermission { RoleId = role.Id, PermissionCode = code })); await db.SaveChangesAsync(ct);
        return Result.Success(new RoleDto(role.Id, role.ServerId, role.Name, role.Position, codes));
    }

    public async Task<Result> SetChannelOverrideAsync(SetChannelOverrideCommand command, CancellationToken ct)
    {
        if (command.PermissionCode is not ("channel.read" or "channel.send") || command.Effect is < 1 or > 2 || (command.RoleId is null) == (command.UserId is null) || !await CanManageAsync(command.ActorUserId, command.ServerId, "channel.manage", ct)) return Result.Failure(CommunityErrors.Forbidden);
        if (!await db.Channels.AnyAsync(x => x.ServerId == command.ServerId && x.SpaceId == command.SpaceId, ct)) return Result.Failure(CommunityErrors.NotFound);
        if (command.RoleId is { } roleId)
        {
            var item = await db.ChannelRoleOverrides.SingleOrDefaultAsync(x => x.SpaceId == command.SpaceId && x.RoleId == roleId && x.PermissionCode == command.PermissionCode, ct);
            if (item is null) db.ChannelRoleOverrides.Add(new ChannelRoleOverride { SpaceId = command.SpaceId, RoleId = roleId, PermissionCode = command.PermissionCode, Effect = (PermissionEffect)command.Effect }); else item.Effect = (PermissionEffect)command.Effect;
            await db.SaveChangesAsync(ct); await RevokeServerAsync(Guid.Empty, command.ServerId, ct);
        }
        else
        {
            var userId = command.UserId!.Value; var item = await db.ChannelUserOverrides.SingleOrDefaultAsync(x => x.SpaceId == command.SpaceId && x.UserId == userId && x.PermissionCode == command.PermissionCode, ct);
            if (item is null) db.ChannelUserOverrides.Add(new ChannelUserOverride { SpaceId = command.SpaceId, UserId = userId, PermissionCode = command.PermissionCode, Effect = (PermissionEffect)command.Effect }); else item.Effect = (PermissionEffect)command.Effect;
            await db.SaveChangesAsync(ct); await revoker.RevokeAsync(userId, [command.SpaceId], ct);
        }
        return Result.Success();
    }

    public async Task<Result> ArchiveChannelAsync(Guid actor, Guid serverId, Guid spaceId, bool deleted, CancellationToken ct)
    {
        if (!await CanManageAsync(actor, serverId, "channel.manage", ct)) return Result.Failure(CommunityErrors.Forbidden);
        var channel = await db.Channels.SingleOrDefaultAsync(x => x.ServerId == serverId && x.SpaceId == spaceId, ct);
        if (channel is null) return Result.Failure(CommunityErrors.NotFound);
        if (deleted) { db.Channels.Remove(channel); await db.SaveChangesAsync(ct); await spaces.RetireAsync(spaceId, ct); }
        else await spaces.ArchiveAsync(spaceId, ct);
        await RevokeServerAsync(Guid.Empty, serverId, ct); return Result.Success();
    }

    private async Task<Result> RemoveAsync(Guid actor, Guid serverId, Guid target, MemberStatus status, bool needsManage, string? reason, CancellationToken ct)
    {
        if (actor == Guid.Empty || target == Guid.Empty || (needsManage && !await CanManageAsync(actor, serverId, "member.manage", ct))) return Result.Failure(CommunityErrors.Forbidden);
        var server = await db.Servers.SingleOrDefaultAsync(x => x.Id == serverId, ct); if (server is null || server.OwnerUserId == target) return Result.Failure(CommunityErrors.Forbidden);
        var member = await db.Members.SingleOrDefaultAsync(x => x.ServerId == serverId && x.UserId == target && x.Status == MemberStatus.Active, ct); if (member is null) return Result.Failure(CommunityErrors.NotFound);
        member.Status = status; member.LeftAt = clock.GetUtcNow();
        if (status == MemberStatus.Banned) db.Bans.Add(new Ban { ServerId = serverId, UserId = target, BannedByUserId = actor, Reason = Normalize(reason, 500), CreatedAt = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct); await RevokeServerAsync(target, serverId, ct); return Result.Success();
    }

    private async Task<bool> CanManageAsync(Guid userId, Guid serverId, string permission, CancellationToken ct)
    {
        var server = await db.Servers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == serverId && x.Status == ServerStatus.Active, ct);
        if (server is null || !await IsActiveMemberAsync(userId, serverId, ct)) return false;
        if (server.OwnerUserId == userId) return true;
        return await (from mr in db.MemberRoles.AsNoTracking() join rp in db.RolePermissions.AsNoTracking() on mr.RoleId equals rp.RoleId where mr.ServerId == serverId && mr.UserId == userId && (rp.PermissionCode == permission || rp.PermissionCode == "server.manage") select rp.RoleId).AnyAsync(ct);
    }
    private Task<bool> IsActiveMemberAsync(Guid userId, Guid serverId, CancellationToken ct) => db.Members.AsNoTracking().AnyAsync(x => x.ServerId == serverId && x.UserId == userId && x.Status == MemberStatus.Active, ct);
    private async Task RevokeServerAsync(Guid userId, Guid serverId, CancellationToken ct)
    {
        var spaces = await db.Channels.AsNoTracking().Where(x => x.ServerId == serverId).Select(x => x.SpaceId).ToArrayAsync(ct);
        var usersToRevoke = userId == Guid.Empty ? await db.Members.AsNoTracking().Where(x => x.ServerId == serverId).Select(x => x.UserId).ToArrayAsync(ct) : [userId];
        foreach (var id in usersToRevoke) await revoker.RevokeAsync(id, spaces, ct);
    }
    private async Task<string> NextSlugAsync(string name, CancellationToken ct) { var baseSlug = string.Concat(name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-'); if (baseSlug.Length < 2) baseSlug = "server"; baseSlug = baseSlug[..Math.Min(baseSlug.Length, 90)]; var candidate = baseSlug; var i = 2; while (await db.Servers.AnyAsync(x => x.Slug == candidate, ct)) candidate = $"{baseSlug}-{i++}"; return candidate; }
    private static bool TryName(string? value, int min, int max, out string normalized) { normalized = Normalize(value, max) ?? string.Empty; return normalized.Length >= min; }
    private static bool TryChannelName(string? value, out string normalized) { normalized = string.Concat((value ?? string.Empty).Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '-')).Trim('-'); return normalized.Length is >= 1 and <= 100; }
    private static string? Normalize(string? value, int max) { var text = value?.Trim(); return string.IsNullOrEmpty(text) ? null : text[..Math.Min(text.Length, max)]; }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static ServerDto ToDto(Server s) => new(s.Id, s.Name, s.Slug, s.Description, s.OwnerUserId, (short)s.Status);
    private static ChannelDto ToDto(Channel c, SCDC.Contracts.Community.ChannelAccessDecision a) => new(c.SpaceId, c.ServerId, c.Name, c.Topic, (short)c.Visibility, c.Position, a.CanRead, a.CanSend);
}
