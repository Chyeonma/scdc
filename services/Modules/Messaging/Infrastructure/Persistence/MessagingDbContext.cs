using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Messaging.Domain;

namespace SCDC.Modules.Messaging.Infrastructure.Persistence;

internal sealed class MessagingDbContext(DbContextOptions<MessagingDbContext> options)
    : DbContext(options)
{
    public DbSet<ChatSpace> Spaces => Set<ChatSpace>();
    public DbSet<DirectConversation> DirectConversations => Set<DirectConversation>();
    public DbSet<GroupConversation> GroupConversations => Set<GroupConversation>();
    public DbSet<SpaceMember> SpaceMembers => Set<SpaceMember>();
    public DbSet<SpaceUserState> SpaceUserStates => Set<SpaceUserState>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureSpace(modelBuilder);
        ConfigureDirectConversation(modelBuilder);
        ConfigureGroupConversation(modelBuilder);
        ConfigureSpaceMember(modelBuilder);
        ConfigureSpaceUserState(modelBuilder);
        ConfigureUserBlock(modelBuilder);
        ConfigureMessage(modelBuilder);
        ConfigureOutbox(modelBuilder);
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

    private static void ConfigureGroupConversation(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<GroupConversation>();
        entity.ToTable("group_conversations", "messaging");
        entity.HasKey(conversation => conversation.SpaceId);
        entity.Property(conversation => conversation.SpaceId).HasColumnName("space_id");
        entity.Property(conversation => conversation.Name).HasColumnName("name").HasMaxLength(100);
        entity.Property(conversation => conversation.AvatarObjectKey).HasColumnName("avatar_object_key").HasMaxLength(500);
        entity.Property(conversation => conversation.OwnerUserId).HasColumnName("owner_user_id");
        entity.Property(conversation => conversation.MaxMembers).HasColumnName("max_members");
        entity.Property(conversation => conversation.CreatedAt).HasColumnName("created_at");
        entity.Property(conversation => conversation.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne<ChatSpace>()
            .WithOne()
            .HasForeignKey<GroupConversation>(conversation => conversation.SpaceId)
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

    private static void ConfigureMessage(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Message>();
        entity.ToTable("messages", "messaging");
        entity.HasKey(message => message.Id);
        entity.Property(message => message.Id).HasColumnName("id");
        entity.Property(message => message.SequenceNo).HasColumnName("sequence_no").ValueGeneratedOnAdd();
        entity.Property(message => message.SpaceId).HasColumnName("space_id");
        entity.Property(message => message.AuthorUserId).HasColumnName("author_user_id");
        entity.Property(message => message.ClientMessageId).HasColumnName("client_message_id");
        entity.Property(message => message.ThreadRootId).HasColumnName("thread_root_id");
        entity.Property(message => message.MessageType).HasColumnName("message_type").HasConversion<short>();
        entity.Property(message => message.Content).HasColumnName("content");
        entity.Property(message => message.IdempotencyPayloadHash)
            .HasColumnName("idempotency_payload_hash")
            .HasMaxLength(64);
        entity.Property(message => message.Version).HasColumnName("version");
        entity.Property(message => message.CreatedAt).HasColumnName("created_at");
        entity.Property(message => message.EditedAt).HasColumnName("edited_at");
        entity.Property(message => message.DeletedAt).HasColumnName("deleted_at");
        entity.HasIndex(message => new { message.SpaceId, message.AuthorUserId, message.ClientMessageId })
            .HasDatabaseName("ux_messages_client_id")
            .IsUnique()
            .HasFilter("author_user_id IS NOT NULL AND client_message_id IS NOT NULL");
    }

    private static void ConfigureOutbox(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OutboxEvent>();
        entity.ToTable("outbox_events", "integration");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.EventType).HasColumnName("event_type").HasMaxLength(100);
        entity.Property(item => item.AggregateType).HasColumnName("aggregate_type").HasMaxLength(50);
        entity.Property(item => item.AggregateId).HasColumnName("aggregate_id");
        entity.Property(item => item.AggregateVersion).HasColumnName("aggregate_version");
        entity.Property(item => item.SpaceId).HasColumnName("space_id");
        entity.Property(item => item.Payload).HasColumnName("payload").HasColumnType("jsonb");
        entity.Property(item => item.OccurredAt).HasColumnName("occurred_at");
        entity.Property(item => item.AvailableAt).HasColumnName("available_at");
        entity.Property(item => item.PublishedAt).HasColumnName("published_at");
        entity.Property(item => item.AttemptCount).HasColumnName("attempt_count");
        entity.Property(item => item.LastError).HasColumnName("last_error");
    }
}
