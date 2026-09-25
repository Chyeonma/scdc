using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class AttachmentDownloadService(MessagingDbContext db,
    IUserDirectory users, SpaceMessageAccess spaceAccess,
    IAttachmentObjectStore store, ILogger<AttachmentDownloadService> logger) : IAttachmentDownloadService
{
    public async Task<Result<AttachmentDownloadDto>> DownloadAsync(Guid actorUserId, Guid spaceId,
        Guid attachmentId, CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty || spaceId == Guid.Empty || attachmentId == Guid.Empty)
            return Result.Failure<AttachmentDownloadDto>(MessagingErrors.ResourceNotFound);
        if (await users.FindByIdAsync(actorUserId, cancellationToken) is null)
            return Result.Failure<AttachmentDownloadDto>(MessagingErrors.ResourceNotFound);

        var space = await db.Spaces.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == spaceId && item.Status != SpaceStatus.Deleted, cancellationToken);
        if (space is null || !(await spaceAccess.CheckAsync(actorUserId, space, cancellationToken)).CanRead)
            return Result.Failure<AttachmentDownloadDto>(MessagingErrors.ResourceNotFound);

        var file = await (from attachment in db.MessageAttachments.AsNoTracking()
            join message in db.Messages.AsNoTracking() on attachment.MessageId equals message.Id
            where attachment.Id == attachmentId && message.SpaceId == spaceId
                  && message.DeletedAt == null && attachment.DeletedAt == null
                  && attachment.ScanStatus == 1
            select attachment).SingleOrDefaultAsync(cancellationToken);
        if (file is null || file.BucketName != store.BucketName)
            return Result.Failure<AttachmentDownloadDto>(MessagingErrors.ResourceNotFound);

        try
        {
            var content = await store.GetAsync(file.ObjectKey, cancellationToken);
            return Result.Success(new AttachmentDownloadDto(content, file.OriginalName, file.MimeType));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not read attachment {AttachmentId}", attachmentId);
            return Result.Failure<AttachmentDownloadDto>(MessagingErrors.AttachmentStorageUnavailable);
        }
    }
}
