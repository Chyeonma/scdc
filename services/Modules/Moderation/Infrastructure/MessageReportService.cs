using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Moderation.Application;

namespace SCDC.Modules.Moderation.Infrastructure;

internal sealed class MessageReportService(ModerationDbContext db, IModerationMessageGateway messaging,
    IPlatformReviewAccess platform,
    TimeProvider clock) : IMessageReportService
{
    private static readonly HashSet<string> Reasons = ["spam", "harassment", "inappropriate", "security", "other"];
    private static readonly Error Invalid = Error.Validation("Moderation.InvalidReport", "Choose a valid reason and keep details under 1000 characters.");
    private static readonly Error NotFound = Error.NotFound("Moderation.ReportNotFound", "The report was not found.");
    private static readonly Error Forbidden = Error.Forbidden("Moderation.ReviewForbidden", "You cannot review this space.");
    private static readonly Error Duplicate = Error.Conflict("Moderation.AlreadyReported", "You already reported this message.");
    private static readonly Error RateLimited = Error.TooManyRequests("Moderation.ReportRateLimited", "Too many reports. Try again later.");
    private static readonly Error AlreadyResolved = Error.Conflict("Moderation.AlreadyResolved", "The report was already resolved.");
    private static readonly Error RemoveFailed = Error.Conflict("Moderation.RemoveFailed", "The message could not be removed. Refresh and try again.");

    public async Task<Result<ReportReceipt>> ReportAsync(Guid reporterId, Guid spaceId, Guid messageId,
        string? reasonCode, string? details, CancellationToken ct)
    {
        if (reporterId == Guid.Empty || spaceId == Guid.Empty || messageId == Guid.Empty
            || reasonCode is null || !Reasons.Contains(reasonCode)
            || details?.Length > 1000)
            return Result.Failure<ReportReceipt>(Invalid);
        var evidence = await messaging.GetReportableAsync(reporterId, spaceId, messageId, ct);
        if (evidence is null) return Result.Failure<ReportReceipt>(NotFound);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Serialize one reporter's submissions so concurrent requests cannot bypass the daily cap.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext('moderation.report'), hashtext({reporterId.ToString()}))", ct);
        if (await db.Reports.AsNoTracking().AnyAsync(x => x.ReporterUserId == reporterId && x.MessageId == messageId, ct))
            return Result.Failure<ReportReceipt>(Duplicate);
        var now = clock.GetUtcNow();
        if (await db.Reports.AsNoTracking().CountAsync(x => x.ReporterUserId == reporterId && x.CreatedAt >= now.AddDays(-1), ct) >= 10)
            return Result.Failure<ReportReceipt>(RateLimited);
        var report = new MessageReport
        {
            Id = Guid.CreateVersion7(), SpaceId = spaceId, MessageId = messageId,
            ReporterUserId = reporterId, ReasonCode = reasonCode,
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
            MessageSnapshot = JsonSerializer.Serialize(new
            {
                evidence.MessageId, evidence.AuthorUserId, evidence.MessageType,
                evidence.Content, evidence.CreatedAt, evidence.Version
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Status = 0, CreatedAt = now
        };
        db.Reports.Add(report);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Result.Failure<ReportReceipt>(Duplicate); }
        await transaction.CommitAsync(ct);
        return Result.Success(new ReportReceipt(report.Id, messageId, now));
    }

    public async Task<Result<IReadOnlyList<ReviewReport>>> ListPendingAsync(Guid moderatorId, Guid spaceId, CancellationToken ct)
    {
        var access = await messaging.GetReviewAccessAsync(moderatorId, spaceId, ct);
        if (access is null) return Result.Failure<IReadOnlyList<ReviewReport>>(Forbidden);
        var reports = await db.Reports.AsNoTracking().Where(x => x.SpaceId == spaceId && x.Status <= 1)
            .OrderBy(x => x.CreatedAt).Take(100).ToListAsync(ct);
        if (reports.Count > 0)
        {
            var now = clock.GetUtcNow();
            db.Actions.AddRange(reports.Select(x => new ModerationAction
            {
                Id = Guid.CreateVersion7(), ServerId = access.ServerId, ModeratorUserId = moderatorId,
                TargetMessageId = x.MessageId, ActionType = "review_view",
                Metadata = JsonSerializer.Serialize(new { reportId = x.Id }), CreatedAt = now
            }));
            await db.SaveChangesAsync(ct);
        }
        return Result.Success<IReadOnlyList<ReviewReport>>(reports.Select(ToDto).ToArray());
    }

    public Task<bool> IsPlatformReviewerAsync(Guid userId, CancellationToken ct) =>
        platform.IsReviewerAsync(userId, ct);

    public async Task<Result<IReadOnlyList<ReviewReport>>> ListGlobalPendingAsync(Guid reviewerId, CancellationToken ct)
    {
        if (!await platform.IsReviewerAsync(reviewerId, ct))
            return Result.Failure<IReadOnlyList<ReviewReport>>(Forbidden);
        var reports = await db.Reports.AsNoTracking().Where(x => x.Status <= 1)
            .OrderBy(x => x.CreatedAt).Take(100).ToListAsync(ct);
        if (reports.Count > 0)
        {
            var now = clock.GetUtcNow();
            db.Actions.AddRange(reports.Select(x => new ModerationAction
            {
                Id = Guid.CreateVersion7(), ModeratorUserId = reviewerId,
                TargetMessageId = x.MessageId, ActionType = "review_view",
                Metadata = JsonSerializer.Serialize(new { reportId = x.Id }), CreatedAt = now
            }));
            await db.SaveChangesAsync(ct);
        }
        return Result.Success<IReadOnlyList<ReviewReport>>(reports.Select(ToDto).ToArray());
    }

    public async Task<Result<ReviewReport>> ResolveAsync(Guid moderatorId, Guid spaceId, Guid reportId,
        string? decision, string? note, CancellationToken ct)
    {
        if (decision is not ("dismiss" or "remove") || note?.Length > 1000)
            return Result.Failure<ReviewReport>(Invalid);
        var access = await messaging.GetReviewAccessAsync(moderatorId, spaceId, ct);
        if (access is null) return Result.Failure<ReviewReport>(Forbidden);
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            var report = await db.Reports.FromSqlInterpolated($"SELECT * FROM moderation.message_reports WHERE id = {reportId} AND space_id = {spaceId} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (report is null) return Result.Failure<ReviewReport>(NotFound);
            if (report.Status is 2 or 3 || (report.Status == 1 && decision != "remove"))
                return Result.Failure<ReviewReport>(AlreadyResolved);
            if (report.Status == 0)
            {
                var now = clock.GetUtcNow();
                report.Status = decision == "remove" ? (short)1 : (short)3;
                report.ReviewedByUserId = moderatorId;
                report.ResolutionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
                if (decision == "dismiss") report.ResolvedAt = now;
                // A report has one decision action; its ID makes remove retries idempotent.
                db.Actions.Add(new ModerationAction
                {
                    Id = report.Id, ServerId = access.ServerId, ModeratorUserId = moderatorId,
                    TargetMessageId = report.MessageId,
                    ActionType = decision == "remove" ? "remove_requested" : "dismiss_report",
                    Reason = report.ResolutionNote, Metadata = JsonSerializer.Serialize(new { reportId }), CreatedAt = now
                });
                await db.SaveChangesAsync(ct);
            }
            await transaction.CommitAsync(ct);
            if (decision == "dismiss") return Result.Success(ToDto(report));
        }

        // The request is durable before Messaging performs its own transactional soft delete/outbox write.
        // If the process stops after deletion, retrying this report finishes the audit record.
        var pending = await db.Reports.AsNoTracking().SingleAsync(x => x.Id == reportId, ct);
        if (!await messaging.RemoveAsync(moderatorId, spaceId, pending.MessageId, ct))
            return Result.Failure<ReviewReport>(RemoveFailed);
        db.ChangeTracker.Clear();
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            var report = await db.Reports.FromSqlInterpolated($"SELECT * FROM moderation.message_reports WHERE id = {reportId} AND space_id = {spaceId} FOR UPDATE")
                .SingleAsync(ct);
            if (report.Status == 1)
            {
                report.Status = 2;
                report.ReviewedByUserId = moderatorId;
                report.ResolvedAt = clock.GetUtcNow();
                var action = await db.Actions.SingleAsync(x => x.Id == reportId, ct);
                action.ActionType = "remove_message";
                await db.SaveChangesAsync(ct);
            }
            await transaction.CommitAsync(ct);
            return Result.Success(ToDto(report));
        }
    }

    private static ReviewReport ToDto(MessageReport x) => new(x.Id, x.SpaceId, x.MessageId,
        x.ReporterUserId, x.ReasonCode, x.Details, x.MessageSnapshot, x.Status,
        x.ReviewedByUserId, x.ResolutionNote, x.CreatedAt, x.ResolvedAt);
}
