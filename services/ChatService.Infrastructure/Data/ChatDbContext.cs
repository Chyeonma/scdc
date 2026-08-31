using ChatService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Data;

public sealed class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ChatServer> Servers => Set<ChatServer>();
    public DbSet<ServerMember> ServerMembers => Set<ServerMember>();
    public DbSet<ChatChannel> Channels => Set<ChatChannel>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder);
        ConfigureRefreshTokens(modelBuilder);
        ConfigureServers(modelBuilder);
        ConfigureServerMembers(modelBuilder);
        ConfigureChannels(modelBuilder);
        ConfigureMessages(modelBuilder);
        ConfigureOutbox(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();
        entity.ToTable("users", table =>
        {
            table.HasCheckConstraint("ck_users_username", "username ~ '^[A-Za-z0-9_.]{3,32}$'");
            table.HasCheckConstraint("ck_users_display_name", "char_length(btrim(display_name)) BETWEEN 1 AND 64");
        });
        entity.HasKey(x => x.Id).HasName("pk_users");
        entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        entity.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(254).IsRequired();
        entity.Property(x => x.Username).HasColumnName("username").HasMaxLength(32).IsRequired();
        entity.Property(x => x.NormalizedUsername).HasColumnName("normalized_username").HasMaxLength(32).IsRequired();
        entity.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(64).IsRequired();
        entity.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
        entity.Property(x => x.SecurityStamp).HasColumnName("security_stamp").HasMaxLength(64).IsRequired();
        entity.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp").HasMaxLength(64).IsRequired().IsConcurrencyToken();
        entity.Property(x => x.AccessFailedCount).HasColumnName("access_failed_count");
        entity.Property(x => x.LockoutEnd).HasColumnName("lockout_end");
        entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        entity.HasIndex(x => x.NormalizedEmail).IsUnique().HasDatabaseName("ux_users_normalized_email");
        entity.HasIndex(x => x.NormalizedUsername).IsUnique().HasDatabaseName("ux_users_normalized_username");
    }

    private static void ConfigureRefreshTokens(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RefreshToken>();
        entity.ToTable("refresh_tokens", table =>
            table.HasCheckConstraint("ck_refresh_tokens_expiry", "expires_at > created_at"));
        entity.HasKey(x => x.Id).HasName("pk_refresh_tokens");
        entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(x => x.UserId).HasColumnName("user_id");
        entity.Property(x => x.FamilyId).HasColumnName("family_id");
        entity.Property(x => x.ParentId).HasColumnName("parent_id");
        entity.Property(x => x.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
        entity.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsFixedLength().IsRequired();
        entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        entity.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        entity.Property(x => x.UsedAt).HasColumnName("used_at");
        entity.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        entity.Property(x => x.RevokeReason).HasColumnName("revoke_reason").HasMaxLength(100);
        entity.Property(x => x.CreatedByIp).HasColumnName("created_by_ip").HasColumnType("inet");
        entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<RefreshToken>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<RefreshToken>().WithMany().HasForeignKey(x => x.ReplacedByTokenId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("ux_refresh_tokens_hash");
        entity.HasIndex(x => x.FamilyId).HasFilter("revoked_at IS NULL").HasDatabaseName("ix_refresh_tokens_active_family");
        entity.HasIndex(x => x.UserId).HasDatabaseName("ix_refresh_tokens_user");
    }

    private static void ConfigureServers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ChatServer>();
        entity.ToTable("servers", table =>
            table.HasCheckConstraint("ck_servers_name", "char_length(btrim(name)) BETWEEN 2 AND 100"));
        entity.HasKey(x => x.Id).HasName("pk_servers");
        entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        entity.Property(x => x.OwnerUserId).HasColumnName("owner_user_id");
        entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => x.OwnerUserId).HasDatabaseName("ix_servers_owner");
    }

    private static void ConfigureServerMembers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ServerMember>();
        entity.ToTable("server_members");
        entity.HasKey(x => new { x.ServerId, x.UserId }).HasName("pk_server_members");
        entity.Property(x => x.ServerId).HasColumnName("server_id");
        entity.Property(x => x.UserId).HasColumnName("user_id");
        entity.Property(x => x.JoinedAt).HasColumnName("joined_at");
        entity.HasOne<ChatServer>().WithMany().HasForeignKey(x => x.ServerId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => new { x.UserId, x.ServerId }).HasDatabaseName("ix_server_members_user");
    }

    private static void ConfigureChannels(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ChatChannel>();
        entity.ToTable("channels", table =>
            table.HasCheckConstraint("ck_channels_name", "name ~ '^[a-z0-9-]{2,100}$'"));
        entity.HasKey(x => x.Id).HasName("pk_channels");
        entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(x => x.ServerId).HasColumnName("server_id");
        entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        entity.HasOne<ChatServer>().WithMany().HasForeignKey(x => x.ServerId).OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(x => new { x.ServerId, x.Name }).IsUnique().HasDatabaseName("ux_channels_server_name");
    }

    private static void ConfigureMessages(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ChatMessage>();
        entity.ToTable("messages", table =>
        {
            table.HasCheckConstraint("ck_messages_content", "char_length(content) BETWEEN 1 AND 2000 AND btrim(content) <> ''");
            table.HasCheckConstraint("ck_messages_version", "version >= 1");
        });
        entity.HasKey(x => x.Id).HasName("pk_messages");
        entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(x => x.ChannelId).HasColumnName("channel_id");
        entity.Property(x => x.AuthorUserId).HasColumnName("author_user_id");
        entity.Property(x => x.ClientMessageId).HasColumnName("client_message_id");
        entity.Property(x => x.Content).HasColumnName("content").HasMaxLength(2000).IsRequired();
        entity.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        entity.Property(x => x.EditedAt).HasColumnName("edited_at");
        entity.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        entity.HasOne<ChatChannel>().WithMany().HasForeignKey(x => x.ChannelId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<User>().WithMany().HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => new { x.AuthorUserId, x.ClientMessageId }).IsUnique().HasDatabaseName("ux_messages_client_id");
        entity.HasIndex(x => new { x.ChannelId, x.CreatedAt, x.Id })
            .IsDescending(false, true, true)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_messages_channel_history");
    }

    private static void ConfigureOutbox(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OutboxEvent>();
        entity.ToTable("outbox_events");
        entity.HasKey(x => x.Id).HasName("pk_outbox_events");
        entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        entity.Property(x => x.AggregateId).HasColumnName("aggregate_id");
        entity.Property(x => x.AggregateVersion).HasColumnName("aggregate_version");
        entity.Property(x => x.ChannelId).HasColumnName("channel_id");
        entity.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        entity.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        entity.Property(x => x.AvailableAt).HasColumnName("available_at");
        entity.Property(x => x.PublishedAt).HasColumnName("published_at");
        entity.Property(x => x.AttemptCount).HasColumnName("attempt_count");
        entity.Property(x => x.LastError).HasColumnName("last_error");
        entity.HasIndex(x => new { x.AvailableAt, x.OccurredAt })
            .HasFilter("published_at IS NULL")
            .HasDatabaseName("ix_outbox_pending");
    }
}
