using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Messaging.Application;

public sealed record AttachmentUploadDto(Guid Id, string Name, string MimeType, string SizeBytes,
    string ChecksumSha256, short ScanStatus, DateTimeOffset ExpiresAt);

public sealed record AttachmentDownloadDto(byte[] Content, string Name, string MimeType);

public interface IAttachmentDownloadService
{
    Task<Result<AttachmentDownloadDto>> DownloadAsync(Guid actorUserId, Guid spaceId,
        Guid attachmentId, CancellationToken cancellationToken);
}

public interface IAttachmentUploadService
{
    Task<Result<AttachmentUploadDto>> UploadAsync(Guid actorUserId, Guid spaceId, Guid clientUploadId,
        string fileName, long declaredLength, string? expectedSha256, Stream content,
        CancellationToken cancellationToken);
}
