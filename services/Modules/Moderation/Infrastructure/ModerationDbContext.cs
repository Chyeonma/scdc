using Microsoft.EntityFrameworkCore;

namespace SCDC.Modules.Moderation.Infrastructure;

internal sealed class MessageReport
{
    public Guid Id { get; set; }
    public Guid SpaceId { get; set; }
    public Guid MessageId { get; set; }
    public Guid ReporterUserId { get; set; }
    public string ReasonCode { get; set; } = "";
    public string? Details { get; set; }
    public string MessageSnapshot { get; set; } = "{}";
    public short Status { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

internal sealed class ModerationAction
{
    public Guid Id { get; set; }
    public Guid? ServerId { get; set; }
    public Guid ModeratorUserId { get; set; }
    public Guid? TargetUserId { get; set; }
    public Guid? TargetMessageId { get; set; }
    public string ActionType { get; set; } = "";
    public string? Reason { get; set; }
    public string Metadata { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

internal sealed class PlatformReviewer
{
    public Guid UserId { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
}

internal sealed class ModerationDbContext(DbContextOptions<ModerationDbContext> options) : DbContext(options)
{
    public DbSet<MessageReport> Reports => Set<MessageReport>();
    public DbSet<ModerationAction> Actions => Set<ModerationAction>();
    public DbSet<PlatformReviewer> Reviewers => Set<PlatformReviewer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var reviewer = modelBuilder.Entity<PlatformReviewer>();
        reviewer.ToTable("reviewers", "moderation");
        reviewer.HasKey(x => x.UserId);
        reviewer.Property(x => x.UserId).HasColumnName("user_id");
        reviewer.Property(x => x.GrantedAt).HasColumnName("granted_at");

        var report = modelBuilder.Entity<MessageReport>();
        report.ToTable("message_reports", "moderation");
        report.HasKey(x => x.Id);
        report.Property(x => x.Id).HasColumnName("id");
        report.Property(x => x.SpaceId).HasColumnName("space_id");
        report.Property(x => x.MessageId).HasColumnName("message_id");
        report.Property(x => x.ReporterUserId).HasColumnName("reporter_user_id");
        report.Property(x => x.ReasonCode).HasColumnName("reason_code").HasMaxLength(50);
        report.Property(x => x.Details).HasColumnName("details").HasMaxLength(1000);
        report.Property(x => x.MessageSnapshot).HasColumnName("message_snapshot").HasColumnType("jsonb");
        report.Property(x => x.Status).HasColumnName("status");
        report.Property(x => x.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
        report.Property(x => x.ResolutionNote).HasColumnName("resolution_note").HasMaxLength(1000);
        report.Property(x => x.CreatedAt).HasColumnName("created_at");
        report.Property(x => x.ResolvedAt).HasColumnName("resolved_at");
        report.HasIndex(x => new { x.MessageId, x.ReporterUserId }).IsUnique()
            .HasDatabaseName("ux_message_reports_reporter");

        var action = modelBuilder.Entity<ModerationAction>();
        action.ToTable("actions", "moderation");
        action.HasKey(x => x.Id);
        action.Property(x => x.Id).HasColumnName("id");
        action.Property(x => x.ServerId).HasColumnName("server_id");
        action.Property(x => x.ModeratorUserId).HasColumnName("moderator_user_id");
        action.Property(x => x.TargetUserId).HasColumnName("target_user_id");
        action.Property(x => x.TargetMessageId).HasColumnName("target_message_id");
        action.Property(x => x.ActionType).HasColumnName("action_type").HasMaxLength(50);
        action.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(1000);
        action.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        action.Property(x => x.CreatedAt).HasColumnName("created_at");
        action.Property(x => x.ExpiresAt).HasColumnName("expires_at");
    }
}
