using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.Modules.Moderation.Application;

namespace SCDC.Api.Controllers.Moderation;

[Authorize]
[Route("api/v1/spaces/{spaceId:guid}/message-reports")]
public sealed class MessageReportsController(IMessageReportService reports) : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReportReceipt>> Report(Guid spaceId, ReportMessageRequest request,
        CancellationToken cancellationToken) => FromResult(await reports.ReportAsync(
            User.GetUserId(), spaceId, request.MessageId, request.ReasonCode, request.Details, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReviewReport>>> Pending(Guid spaceId,
        CancellationToken cancellationToken) => FromResult(await reports.ListPendingAsync(
            User.GetUserId(), spaceId, cancellationToken));

    [HttpPost("{reportId:guid}/resolve")]
    public async Task<ActionResult<ReviewReport>> Resolve(Guid spaceId, Guid reportId,
        ResolveReportRequest request, CancellationToken cancellationToken) => FromResult(await reports.ResolveAsync(
            User.GetUserId(), spaceId, reportId, request.Decision, request.Note, cancellationToken));
}

public sealed record ReportMessageRequest(Guid MessageId, string? ReasonCode, string? Details);
public sealed record ResolveReportRequest(string? Decision, string? Note);
