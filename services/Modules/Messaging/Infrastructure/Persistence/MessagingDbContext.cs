using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Messaging.Domain;

namespace SCDC.Modules.Messaging.Infrastructure.Persistence;

internal sealed class MessagingDbContext(DbContextOptions<MessagingDbContext> options)
    : DbContext(options)
{
    public DbSet<ChatSpace> Spaces => Set<ChatSpace>();
    public DbSet<DirectConversation> DirectConversations => Set<DirectConversation>();
    public DbSet<SpaceMember> SpaceMembers => Set<SpaceMember>();
    public DbSet<SpaceUserState> SpaceUserStates => Set<SpaceUserState>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureSpace(modelBuilder);
        ConfigureDirectConversation(modelBuilder);
        ConfigureSpaceMember(modelBuilder);
        ConfigureSpaceUserState(modelBuilder);
        ConfigureUserBlock(modelBuilder);
    }

    private static void ConfigureSpace(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ChatSpace>();
        entity.ToTable("spaces", "messaging");
        entity.HasKey(space => space.Id);
        entity.Property(space => space.Id).HasColumnName("id");
        entity.Property(space => space.SpaceType).HasColumnName("space_type").HasConversion<short>();
        entity.Property(space => space.Status).HasColumnName("status").HasConversion<short>();
        entity.Property(space => space.CreatedByUserId).HasColumnName("created_by_user_id");
        entity.Property(space => space.LastMessageId).HasColumnName("last_message_id");
        entity.Property(space => space.LastMessageSequence).HasColumnName("last_message_sequence");
        entity.Property(space => space.LastActivityAt).HasColumnName("last_activity_at");
        entity.Property(space => space.CreatedAt).HasColumnName("created_at");
        entity.Property(space => space.UpdatedAt).HasColumnName("updated_at");
        entity.Property(space => space.ArchivedAt).HasColumnName("archived_at");
        entity.Property(space => space.DeletedAt).HasColumnName("deleted_at");
        entity.Property(space => space.Version).HasColumnName("version").IsConcurrencyToken();
    }

    private static void ConfigureDirectConversation(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<DirectConversation>();
        entity.ToTable("direct_conversations", "messaging");
        entity.HasKey(conversation => conversation.SpaceId);
        entity.Property(conversation => conversation.SpaceId).HasColumnName("space_id");
        entity.Property(conversation => conversation.UserLowId).HasColumnName("user_low_id");
        entity.Property(conversation => conversation.UserHighId).HasColumnName("user_high_id");
        entity.Property(conversation => conversation.CreatedAt).HasColumnName("created_at");
        entity.HasIndex(conversation => new { conversation.UserLowId, conversation.UserHighId })
            .IsUnique()
            .HasDatabaseName("ux_direct_conversations_pair");
        entity.HasOne<ChatSpace>()
            .WithOne()
            .HasForeignKey<DirectConversation>(conversation => conversation.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureSpaceMember(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SpaceMember>();
        entity.ToTable("space_members", "messaging");
        entity.HasKey(member => new { member.SpaceId, member.UserId });
        entity.Property(member => member.SpaceId).HasColumnName("space_id");
        entity.Property(member => member.UserId).HasColumnName("user_id");
        entity.Property(member => member.MemberRole).HasColumnName("member_role").HasConversion<short>();
        entity.Property(member => member.MembershipStatus).HasColumnName("membership_status").HasConversion<short>();
        entity.Property(member => member.JoinedAt).HasColumnName("joined_at");
        entity.Property(member => member.LeftAt).HasColumnName("left_at");
        entity.Property(member => member.RemovedByUserId).HasColumnName("removed_by_user_id");
        entity.HasOne<ChatSpace>()
            .WithMany()
            .HasForeignKey(member => member.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureSpaceUserState(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SpaceUserState>();
        entity.ToTable("space_user_states", "messaging");
        entity.HasKey(state => new { state.SpaceId, state.UserId });
        entity.Property(state => state.SpaceId).HasColumnName("space_id");
        entity.Property(state => state.UserId).HasColumnName("user_id");
        entity.Property(state => state.LastReadSequence).HasColumnName("last_read_sequence");
        entity.Property(state => state.LastReadAt).HasColumnName("last_read_at");
        entity.Property(state => state.NotificationLevel).HasColumnName("notification_level").HasConversion<short>();
        entity.Property(state => state.MutedUntil).HasColumnName("muted_until");
        entity.Property(state => state.IsHidden).HasColumnName("is_hidden");
        entity.Property(state => state.IsPinned).HasColumnName("is_pinned");
        entity.Property(state => state.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne<ChatSpace>()
            .WithMany()
            .HasForeignKey(state => state.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureUserBlock(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserBlock>();
        entity.ToTable("user_blocks", "messaging");
        entity.HasKey(block => new { block.BlockerUserId, block.BlockedUserId });
        entity.Property(block => block.BlockerUserId).HasColumnName("blocker_user_id");
        entity.Property(block => block.BlockedUserId).HasColumnName("blocked_user_id");
        entity.Property(block => block.CreatedAt).HasColumnName("created_at");
    }
}
