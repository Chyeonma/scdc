using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Messaging.Domain;

namespace SCDC.Modules.Messaging.Infrastructure.Persistence;

internal sealed class MessagingDbContext(DbContextOptions<MessagingDbContext> options) : DbContext(options)
{
    public DbSet<ChatSpace> Spaces => Set<ChatSpace>();
    public DbSet<DirectConversation> DirectConversations => Set<DirectConversation>();
    public DbSet<SpaceMember> SpaceMembers => Set<SpaceMember>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        var space = builder.Entity<ChatSpace>();
        space.ToTable("spaces", "messaging");
        space.HasKey(s => s.Id);
        space.HasAlternateKey(s => new { s.Id, s.SpaceType });
        space.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
        space.Property(s => s.SpaceType).HasColumnName("space_type");
        space.Property(s => s.Status).HasColumnName("status");
        space.Property(s => s.CreatedByUserId).HasColumnName("created_by_user_id");
        space.Property(s => s.LastMessageSequence).HasColumnName("last_message_sequence");
        space.Property(s => s.LastActivityAt).HasColumnName("last_activity_at");
        space.Property(s => s.CreatedAt).HasColumnName("created_at");
        space.Property(s => s.DeletedAt).HasColumnName("deleted_at");

        var pair = builder.Entity<DirectConversation>();
        pair.ToTable("direct_conversations", "messaging");
        pair.HasKey(d => d.SpaceId);
        pair.Property(d => d.SpaceId).HasColumnName("space_id").ValueGeneratedNever();
        pair.Property(d => d.SpaceType).HasColumnName("space_type").HasComputedColumnSql("1::smallint", stored: true);
        pair.Property(d => d.UserLowId).HasColumnName("user_low_id");
        pair.Property(d => d.UserHighId).HasColumnName("user_high_id");
        pair.Property(d => d.CreatedAt).HasColumnName("created_at");
        pair.HasIndex(d => new { d.UserLowId, d.UserHighId }).IsUnique().HasDatabaseName("ux_direct_conversations_pair");
        pair.HasOne<ChatSpace>().WithMany().HasForeignKey(d => new { d.SpaceId, d.SpaceType })
            .HasPrincipalKey(s => new { s.Id, s.SpaceType });

        var member = builder.Entity<SpaceMember>();
        member.ToTable("space_members", "messaging");
        member.HasKey(m => new { m.SpaceId, m.UserId });
        member.Property(m => m.SpaceId).HasColumnName("space_id");
        member.Property(m => m.UserId).HasColumnName("user_id");
        member.Property(m => m.MemberRole).HasColumnName("member_role");
        member.Property(m => m.MembershipStatus).HasColumnName("membership_status");
        member.Property(m => m.JoinedAt).HasColumnName("joined_at");
        member.Property(m => m.LeftAt).HasColumnName("left_at");
        member.HasOne<ChatSpace>().WithMany().HasForeignKey(m => m.SpaceId);
    }
}
