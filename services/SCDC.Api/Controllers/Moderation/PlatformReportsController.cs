using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.Modules.Moderation.Application;

namespace SCDC.Api.Controllers.Moderation;

[Authorize]
[Route("api/v1/message-reports")]
public sealed class PlatformReportsController(IMessageReportService reports) : ApiControllerBase
{
    [HttpGet("access")]
    public async Task<ActionResult<PlatformReviewAccessDto>> Access(CancellationToken cancellationToken) =>
        Ok(new PlatformReviewAccessDto(await reports.IsPlatformReviewerAsync(User.GetUserId(), cancellationToken)));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReviewReport>>> Pending(CancellationToken cancellationToken) =>
        FromResult(await reports.ListGlobalPendingAsync(User.GetUserId(), cancellationToken));
}

public sealed record PlatformReviewAccessDto(bool CanReview);
