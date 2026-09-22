namespace SCDC.Modules.Community.Domain;

internal enum ServerStatus : short { Active = 1, Archived = 2, Deleted = 3 }
internal enum MemberStatus : short { Active = 1, Left = 2, Kicked = 3, Banned = 4 }
internal enum ChannelVisibility : short { Public = 1, Private = 2, ReadOnly = 3 }
internal enum PermissionEffect : short { Allow = 1, Deny = 2 }

internal sealed class Server
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ServerStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int Version { get; set; }
}

internal sealed class ServerMember
{
    public Guid ServerId { get; set; }
    public Guid UserId { get; set; }
    public string? Nickname { get; set; }
    public MemberStatus Status { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LeftAt { get; set; }
}

internal sealed class Channel
{
    public Guid SpaceId { get; set; }
    public Guid ServerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Topic { get; set; }
    public ChannelVisibility Visibility { get; set; }
    public int Position { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

internal sealed class Role
{
    public Guid Id { get; set; }
    public Guid ServerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public bool IsDefault { get; set; }
    public bool IsSystem { get; set; }
}

internal sealed class Permission { public string Code { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; }
internal sealed class RolePermission { public Guid RoleId { get; set; } public string PermissionCode { get; set; } = string.Empty; }
internal sealed class MemberRole { public Guid ServerId { get; set; } public Guid UserId { get; set; } public Guid RoleId { get; set; } }
internal sealed class ChannelRoleOverride { public Guid SpaceId { get; set; } public Guid RoleId { get; set; } public string PermissionCode { get; set; } = string.Empty; public PermissionEffect Effect { get; set; } }
internal sealed class ChannelUserOverride { public Guid SpaceId { get; set; } public Guid UserId { get; set; } public string PermissionCode { get; set; } = string.Empty; public PermissionEffect Effect { get; set; } }
internal sealed class Invite { public Guid Id { get; set; } public Guid ServerId { get; set; } public string CodeHash { get; set; } = string.Empty; public Guid CreatedByUserId { get; set; } public int? MaxUses { get; set; } public int UseCount { get; set; } public DateTimeOffset? ExpiresAt { get; set; } public DateTimeOffset? RevokedAt { get; set; } public DateTimeOffset CreatedAt { get; set; } }
internal sealed class Ban { public Guid ServerId { get; set; } public Guid UserId { get; set; } public Guid BannedByUserId { get; set; } public string? Reason { get; set; } public DateTimeOffset CreatedAt { get; set; } public DateTimeOffset? ExpiresAt { get; set; } public DateTimeOffset? RevokedAt { get; set; } }
