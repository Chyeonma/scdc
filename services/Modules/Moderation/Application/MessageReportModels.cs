using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Moderation.Application;

public interface IMessageReportService
{
    Task<Result<ReportReceipt>> ReportAsync(Guid reporterId, Guid spaceId, Guid messageId, string? reasonCode,
        string? details, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ReviewReport>>> ListPendingAsync(Guid moderatorId, Guid spaceId, CancellationToken cancellationToken);
    Task<bool> IsPlatformReviewerAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ReviewReport>>> ListGlobalPendingAsync(Guid reviewerId, CancellationToken cancellationToken);
    Task<Result<ReviewReport>> ResolveAsync(Guid moderatorId, Guid spaceId, Guid reportId, string? decision,
        string? note, CancellationToken cancellationToken);
}

public sealed record ReportReceipt(Guid Id, Guid MessageId, DateTimeOffset CreatedAt);

public sealed record ReviewReport(Guid Id, Guid SpaceId, Guid MessageId, Guid ReporterUserId,
    string ReasonCode, string? Details, string MessageSnapshot, short Status,
    Guid? ReviewedByUserId, string? ResolutionNote, DateTimeOffset CreatedAt, DateTimeOffset? ResolvedAt);
