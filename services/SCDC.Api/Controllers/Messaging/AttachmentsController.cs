using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.Api.Errors;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Controllers.Messaging;

[Authorize]
[Route("api/v1/spaces/{spaceId:guid}/attachments")]
public sealed class AttachmentsController(IAttachmentUploadService uploads,
    IAttachmentDownloadService downloads) : ApiControllerBase
{
    [HttpGet("{attachmentId:guid}/download")]
    public async Task<IActionResult> Download(Guid spaceId, Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var result = await downloads.DownloadAsync(User.GetUserId(), spaceId, attachmentId, cancellationToken);
        if (result.IsFailure) return ApiErrorMapper.ToObjectResult(result.Error, HttpContext);
        Response.Headers.CacheControl = "no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(result.Value.Content, result.Value.MimeType, result.Value.Name);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [ProducesResponseType<AttachmentUploadDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AttachmentUploadDto>> Upload(
        Guid spaceId, IFormFile? file, [FromForm] Guid clientUploadId, [FromForm] string? checksumSha256,
        CancellationToken cancellationToken)
    {
        if (file is null)
            return FromResult(Result.Failure<AttachmentUploadDto>(MessagingErrors.InvalidAttachment));
        await using var content = file.OpenReadStream();
        var result = await uploads.UploadAsync(User.GetUserId(), spaceId, clientUploadId,
            file.FileName, file.Length, checksumSha256, content, cancellationToken);
        if (result.IsFailure && result.Error == MessagingErrors.RateLimited)
            Response.Headers.RetryAfter = "60";
        return FromResult(result);
    }
}
