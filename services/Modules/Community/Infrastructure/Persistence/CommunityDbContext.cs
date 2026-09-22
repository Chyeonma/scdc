using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Community.Domain;

namespace SCDC.Modules.Community.Infrastructure.Persistence;

internal sealed class CommunityDbContext(DbContextOptions<CommunityDbContext> options) : DbContext(options)
{
    public DbSet<Server> Servers => Set<Server>();
    public DbSet<ServerMember> Members => Set<ServerMember>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<MemberRole> MemberRoles => Set<MemberRole>();
    public DbSet<ChannelRoleOverride> ChannelRoleOverrides => Set<ChannelRoleOverride>();
    public DbSet<ChannelUserOverride> ChannelUserOverrides => Set<ChannelUserOverride>();
    public DbSet<Invite> Invites => Set<Invite>();
    public DbSet<Ban> Bans => Set<Ban>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Server>(e => { e.ToTable("servers", "community"); e.HasKey(x => x.Id); e.Property(x => x.Id).HasColumnName("id"); e.Property(x => x.OwnerUserId).HasColumnName("owner_user_id"); e.Property(x => x.Name).HasColumnName("name"); e.Property(x => x.Slug).HasColumnName("slug"); e.Property(x => x.Description).HasColumnName("description"); e.Property(x => x.Status).HasColumnName("status").HasConversion<short>(); e.Property(x => x.CreatedAt).HasColumnName("created_at"); e.Property(x => x.UpdatedAt).HasColumnName("updated_at"); e.Property(x => x.Version).HasColumnName("version"); });
        b.Entity<ServerMember>(e => { e.ToTable("server_members", "community"); e.HasKey(x => new { x.ServerId, x.UserId }); e.Property(x => x.ServerId).HasColumnName("server_id"); e.Property(x => x.UserId).HasColumnName("user_id"); e.Property(x => x.Nickname).HasColumnName("nickname"); e.Property(x => x.Status).HasColumnName("status").HasConversion<short>(); e.Property(x => x.JoinedAt).HasColumnName("joined_at"); e.Property(x => x.LeftAt).HasColumnName("left_at"); });
        b.Entity<Channel>(e => { e.ToTable("channels", "community"); e.HasKey(x => x.SpaceId); e.Property(x => x.SpaceId).HasColumnName("space_id"); e.Property(x => x.ServerId).HasColumnName("server_id"); e.Property(x => x.Name).HasColumnName("name"); e.Property(x => x.Topic).HasColumnName("topic"); e.Property(x => x.Visibility).HasColumnName("visibility").HasConversion<short>(); e.Property(x => x.Position).HasColumnName("position"); e.Property(x => x.CreatedAt).HasColumnName("created_at"); e.Property(x => x.UpdatedAt).HasColumnName("updated_at"); });
        b.Entity<Role>(e => { e.ToTable("roles", "community"); e.HasKey(x => x.Id); e.Property(x => x.Id).HasColumnName("id"); e.Property(x => x.ServerId).HasColumnName("server_id"); e.Property(x => x.Name).HasColumnName("name"); e.Property(x => x.Position).HasColumnName("position"); e.Property(x => x.IsDefault).HasColumnName("is_default"); e.Property(x => x.IsSystem).HasColumnName("is_system"); });
        b.Entity<Permission>(e => { e.ToTable("permissions", "community"); e.HasKey(x => x.Code); e.Property(x => x.Code).HasColumnName("code"); e.Property(x => x.Description).HasColumnName("description"); });
        b.Entity<RolePermission>(e => { e.ToTable("role_permissions", "community"); e.HasKey(x => new { x.RoleId, x.PermissionCode }); e.Property(x => x.RoleId).HasColumnName("role_id"); e.Property(x => x.PermissionCode).HasColumnName("permission_code"); });
        b.Entity<MemberRole>(e => { e.ToTable("member_roles", "community"); e.HasKey(x => new { x.ServerId, x.UserId, x.RoleId }); e.Property(x => x.ServerId).HasColumnName("server_id"); e.Property(x => x.UserId).HasColumnName("user_id"); e.Property(x => x.RoleId).HasColumnName("role_id"); });
        b.Entity<ChannelRoleOverride>(e => { e.ToTable("channel_role_overrides", "community"); e.HasKey(x => new { x.SpaceId, x.RoleId, x.PermissionCode }); e.Property(x => x.SpaceId).HasColumnName("space_id"); e.Property(x => x.RoleId).HasColumnName("role_id"); e.Property(x => x.PermissionCode).HasColumnName("permission_code"); e.Property(x => x.Effect).HasColumnName("effect").HasConversion<short>(); });
        b.Entity<ChannelUserOverride>(e => { e.ToTable("channel_user_overrides", "community"); e.HasKey(x => new { x.SpaceId, x.UserId, x.PermissionCode }); e.Property(x => x.SpaceId).HasColumnName("space_id"); e.Property(x => x.UserId).HasColumnName("user_id"); e.Property(x => x.PermissionCode).HasColumnName("permission_code"); e.Property(x => x.Effect).HasColumnName("effect").HasConversion<short>(); });
        b.Entity<Invite>(e => { e.ToTable("invites", "community"); e.HasKey(x => x.Id); e.Property(x => x.Id).HasColumnName("id"); e.Property(x => x.ServerId).HasColumnName("server_id"); e.Property(x => x.CodeHash).HasColumnName("code_hash"); e.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id"); e.Property(x => x.MaxUses).HasColumnName("max_uses"); e.Property(x => x.UseCount).HasColumnName("use_count"); e.Property(x => x.ExpiresAt).HasColumnName("expires_at"); e.Property(x => x.RevokedAt).HasColumnName("revoked_at"); e.Property(x => x.CreatedAt).HasColumnName("created_at"); });
        b.Entity<Ban>(e => { e.ToTable("bans", "community"); e.HasKey(x => new { x.ServerId, x.UserId }); e.Property(x => x.ServerId).HasColumnName("server_id"); e.Property(x => x.UserId).HasColumnName("user_id"); e.Property(x => x.BannedByUserId).HasColumnName("banned_by_user_id"); e.Property(x => x.Reason).HasColumnName("reason"); e.Property(x => x.CreatedAt).HasColumnName("created_at"); e.Property(x => x.ExpiresAt).HasColumnName("expires_at"); e.Property(x => x.RevokedAt).HasColumnName("revoked_at"); });
    }
}
