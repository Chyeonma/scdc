using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Messaging.Domain;

namespace SCDC.Modules.Messaging.Infrastructure.Persistence;

internal sealed class MessagingDbContext(DbContextOptions<MessagingDbContext> options) : DbContext(options)
{
    public DbSet<ChatSpace> Spaces => Set<ChatSpace>();
    public DbSet<DirectConversation> DirectConversations => Set<DirectConversation>();
    public DbSet<SpaceMember> SpaceMembers => Set<SpaceMember>();
    public DbSet<TextMessage> Messages => Set<TextMessage>();
    public DbSet<SendOperation> SendOperations => Set<SendOperation>();
    public DbSet<MessageOutbox> MessageOutbox => Set<MessageOutbox>();

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
        space.Property(s => s.LastMessageId).HasColumnName("last_message_id");
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

        var message = builder.Entity<TextMessage>();
        message.ToTable("messages", "messaging"); message.HasKey(m => m.Id);
        message.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();
        message.Property(m => m.SpaceId).HasColumnName("space_id");
        message.Property(m => m.AuthorUserId).HasColumnName("author_user_id");
        message.Property(m => m.ClientMessageId).HasColumnName("client_message_id");
        message.Property(m => m.MessageType).HasColumnName("message_type");
        message.Property(m => m.ConversationSequence).HasColumnName("conversation_sequence");
        message.Property(m => m.Version).HasColumnName("version");
        message.Property(m => m.Content).HasColumnName("content");
        message.Property(m => m.CreatedAt).HasColumnName("created_at");
        message.Property(m => m.EditedAt).HasColumnName("edited_at");
        message.Property(m => m.DeletedAt).HasColumnName("deleted_at");

        var operation = builder.Entity<SendOperation>();
        operation.ToTable("send_operations", "messaging");
        operation.HasKey(o => new { o.SpaceId, o.AuthorUserId, o.ClientMessageId });
        operation.Property(o => o.SpaceId).HasColumnName("space_id");
        operation.Property(o => o.AuthorUserId).HasColumnName("author_user_id");
        operation.Property(o => o.ClientMessageId).HasColumnName("client_message_id");
        operation.Property(o => o.MessageId).HasColumnName("message_id");
        operation.Property(o => o.FingerprintVersion).HasColumnName("fingerprint_version");
        operation.Property(o => o.KeyId).HasColumnName("key_id");
        operation.Property(o => o.Fingerprint).HasColumnName("fingerprint");
        operation.Property(o => o.CreatedAt).HasColumnName("created_at");

        var outbox = builder.Entity<MessageOutbox>();
        outbox.ToTable("outbox_events", "integration"); outbox.HasKey(o => o.Id);
        outbox.Property(o => o.Id).HasColumnName("id").ValueGeneratedNever();
        outbox.Property(o => o.EventType).HasColumnName("event_type");
        outbox.Property(o => o.AggregateType).HasColumnName("aggregate_type");
        outbox.Property(o => o.AggregateId).HasColumnName("aggregate_id");
        outbox.Property(o => o.AggregateVersion).HasColumnName("aggregate_version");
        outbox.Property(o => o.SpaceId).HasColumnName("space_id");
        outbox.Property(o => o.Payload).HasColumnName("payload").HasColumnType("jsonb");
        outbox.Property(o => o.OccurredAt).HasColumnName("occurred_at");
        outbox.Property(o => o.AvailableAt).HasColumnName("available_at");
    }
}
