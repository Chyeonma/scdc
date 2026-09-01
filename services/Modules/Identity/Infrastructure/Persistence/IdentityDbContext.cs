using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Identity.Domain;

namespace SCDC.Modules.Identity.Infrastructure.Persistence;

internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserEmail> UserEmails => Set<UserEmail>();
    public DbSet<PasswordCredential> PasswordCredentials => Set<PasswordCredential>();
    public DbSet<UserSecurityState> UserSecurityStates => Set<UserSecurityState>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AccountToken> AccountTokens => Set<AccountToken>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUser(modelBuilder);
        ConfigureProfile(modelBuilder);
        ConfigureEmail(modelBuilder);
        ConfigurePassword(modelBuilder);
        ConfigureSecurityState(modelBuilder);
        ConfigureSession(modelBuilder);
        ConfigureRefreshToken(modelBuilder);
        ConfigureAccountToken(modelBuilder);
        ConfigureSecurityEvent(modelBuilder);
        ConfigureOutbox(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();
        entity.ToTable("users", "identity");
        entity.HasKey(user => user.Id);
        entity.Property(user => user.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        entity.Property(user => user.Username).HasColumnName("username").HasMaxLength(32);
        entity.Property(user => user.NormalizedUsername)
            .HasColumnName("normalized_username")
            .HasMaxLength(32)
            .HasComputedColumnSql("lower(btrim(username))", stored: true);
        entity.Property(user => user.Status).HasColumnName("status").HasConversion<short>();
        entity.Property(user => user.CreatedAt).HasColumnName("created_at");
        entity.Property(user => user.UpdatedAt).HasColumnName("updated_at");
        entity.Property(user => user.DeletedAt).HasColumnName("deleted_at");
        entity.Property(user => user.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate();
        entity.HasIndex(user => user.NormalizedUsername)
            .IsUnique()
            .HasDatabaseName("ux_users_normalized_username");
    }

    private static void ConfigureProfile(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserProfile>();
        entity.ToTable("user_profiles", "identity");
        entity.HasKey(profile => profile.UserId);
        entity.Property(profile => profile.UserId).HasColumnName("user_id");
        entity.Property(profile => profile.DisplayName).HasColumnName("display_name").HasMaxLength(64);
        entity.Property(profile => profile.Bio).HasColumnName("bio").HasMaxLength(500);
        entity.Property(profile => profile.AvatarObjectKey).HasColumnName("avatar_object_key").HasMaxLength(500);
        entity.Property(profile => profile.Locale).HasColumnName("locale").HasMaxLength(16);
        entity.Property(profile => profile.Timezone).HasColumnName("timezone").HasMaxLength(64);
        entity.Property(profile => profile.CreatedAt).HasColumnName("created_at");
        entity.Property(profile => profile.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(profile => profile.User)
            .WithOne(user => user.Profile)
            .HasForeignKey<UserProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureEmail(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserEmail>();
        entity.ToTable("user_emails", "identity");
        entity.HasKey(email => email.Id);
        entity.Property(email => email.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        entity.Property(email => email.UserId).HasColumnName("user_id");
        entity.Property(email => email.Email).HasColumnName("email").HasMaxLength(254);
        entity.Property(email => email.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(254)
            .HasComputedColumnSql("lower(btrim(email))", stored: true);
        entity.Property(email => email.IsPrimary).HasColumnName("is_primary");
        entity.Property(email => email.VerifiedAt).HasColumnName("verified_at");
        entity.Property(email => email.CreatedAt).HasColumnName("created_at");
        entity.Property(email => email.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(email => email.User)
            .WithMany(user => user.Emails)
            .HasForeignKey(email => email.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(email => email.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("ux_user_emails_normalized_email");
    }

    private static void ConfigurePassword(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PasswordCredential>();
        entity.ToTable("password_credentials", "identity");
        entity.HasKey(credential => credential.UserId);
        entity.Property(credential => credential.UserId).HasColumnName("user_id");
        entity.Property(credential => credential.PasswordHash).HasColumnName("password_hash");
        entity.Property(credential => credential.HashAlgorithm).HasColumnName("hash_algorithm").HasMaxLength(50);
        entity.Property(credential => credential.PasswordVersion).HasColumnName("password_version");
        entity.Property(credential => credential.PasswordChangedAt).HasColumnName("password_changed_at");
        entity.Property(credential => credential.RequiresChange).HasColumnName("requires_change");
        entity.Property(credential => credential.CreatedAt).HasColumnName("created_at");
        entity.Property(credential => credential.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(credential => credential.User)
            .WithOne(user => user.PasswordCredential)
            .HasForeignKey<PasswordCredential>(credential => credential.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureSecurityState(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserSecurityState>();
        entity.ToTable("user_security_states", "identity");
        entity.HasKey(state => state.UserId);
        entity.Property(state => state.UserId).HasColumnName("user_id");
        entity.Property(state => state.SecurityStamp).HasColumnName("security_stamp");
        entity.Property(state => state.FailedLoginCount).HasColumnName("failed_login_count");
        entity.Property(state => state.LastFailedLoginAt).HasColumnName("last_failed_login_at");
        entity.Property(state => state.LockedUntil).HasColumnName("locked_until");
        entity.Property(state => state.LastSuccessfulLoginAt).HasColumnName("last_successful_login_at");
        entity.Property(state => state.MfaEnabled).HasColumnName("mfa_enabled");
        entity.Property(state => state.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(state => state.User)
            .WithOne(user => user.SecurityState)
            .HasForeignKey<UserSecurityState>(state => state.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureSession(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AuthSession>();
        entity.ToTable("auth_sessions", "identity");
        entity.HasKey(session => session.Id);
        entity.Property(session => session.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        entity.Property(session => session.UserId).HasColumnName("user_id");
        entity.Property(session => session.DeviceName).HasColumnName("device_name").HasMaxLength(100);
        entity.Property(session => session.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        entity.Property(session => session.CreatedByIp).HasColumnName("created_by_ip").HasColumnType("inet");
        entity.Property(session => session.LastSeenIp).HasColumnName("last_seen_ip").HasColumnType("inet");
        entity.Property(session => session.CreatedAt).HasColumnName("created_at");
        entity.Property(session => session.LastSeenAt).HasColumnName("last_seen_at");
        entity.Property(session => session.ExpiresAt).HasColumnName("expires_at");
        entity.Property(session => session.RevokedAt).HasColumnName("revoked_at");
        entity.Property(session => session.RevokeReason).HasColumnName("revoke_reason").HasMaxLength(100);
        entity.HasOne(session => session.User)
            .WithMany(user => user.Sessions)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureRefreshToken(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RefreshToken>();
        entity.ToTable("refresh_tokens", "identity");
        entity.HasKey(token => token.Id);
        entity.HasAlternateKey(token => new { token.Id, token.SessionId });
        entity.Property(token => token.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        entity.Property(token => token.SessionId).HasColumnName("session_id");
        entity.Property(token => token.ParentTokenId).HasColumnName("parent_token_id");
        entity.Property(token => token.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
        entity.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsFixedLength();
        entity.Property(token => token.CreatedAt).HasColumnName("created_at");
        entity.Property(token => token.ExpiresAt).HasColumnName("expires_at");
        entity.Property(token => token.UsedAt).HasColumnName("used_at");
        entity.Property(token => token.RevokedAt).HasColumnName("revoked_at");
        entity.Property(token => token.RevokeReason).HasColumnName("revoke_reason").HasMaxLength(100);
        entity.HasOne(token => token.Session)
            .WithMany(session => session.RefreshTokens)
            .HasForeignKey(token => token.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(token => new { token.ParentTokenId, token.SessionId })
            .HasPrincipalKey(token => new { token.Id, token.SessionId })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(token => new { token.ReplacedByTokenId, token.SessionId })
            .HasPrincipalKey(token => new { token.Id, token.SessionId })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_refresh_tokens_hash");
    }

    private static void ConfigureAccountToken(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AccountToken>();
        entity.ToTable("account_tokens", "identity");
        entity.HasKey(token => token.Id);
        entity.Property(token => token.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        entity.Property(token => token.UserId).HasColumnName("user_id");
        entity.Property(token => token.Purpose).HasColumnName("purpose").HasConversion<short>();
        entity.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsFixedLength();
        entity.Property(token => token.TargetValue).HasColumnName("target_value").HasMaxLength(254);
        entity.Property(token => token.CreatedByIp).HasColumnName("created_by_ip").HasColumnType("inet");
        entity.Property(token => token.CreatedAt).HasColumnName("created_at");
        entity.Property(token => token.ExpiresAt).HasColumnName("expires_at");
        entity.Property(token => token.ConsumedAt).HasColumnName("consumed_at");
        entity.HasOne(token => token.User)
            .WithMany(user => user.AccountTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_account_tokens_hash");
    }

    private static void ConfigureSecurityEvent(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SecurityEvent>();
        entity.ToTable("security_events", "audit");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        entity.Property(item => item.UserId).HasColumnName("user_id");
        entity.Property(item => item.EventType).HasColumnName("event_type").HasMaxLength(50);
        entity.Property(item => item.IpAddress).HasColumnName("ip_address").HasColumnType("inet");
        entity.Property(item => item.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        entity.Property(item => item.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        entity.Property(item => item.OccurredAt).HasColumnName("occurred_at");
    }

    private static void ConfigureOutbox(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OutboxEvent>();
        entity.ToTable("outbox_events", "integration");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
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
