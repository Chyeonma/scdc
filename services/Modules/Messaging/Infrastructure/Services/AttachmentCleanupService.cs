using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class AttachmentCleanupService(MessagingDbContext db,
    IAttachmentObjectStore store, TimeProvider timeProvider,
    ILogger<AttachmentCleanupService> logger) : IAttachmentCleanupService
{
    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken)
    {
        var expired = await db.AttachmentUploads.AsNoTracking()
            .Where(upload => upload.AttachedMessageId == null && upload.ExpiresAt < timeProvider.GetUtcNow())
            .OrderBy(upload => upload.ExpiresAt).Take(100).ToListAsync(cancellationToken);
        var removed = 0;
        foreach (var upload in expired)
        {
            try
            {
                await store.DeleteAsync(upload.ObjectKey, cancellationToken);
                removed += await db.AttachmentUploads.Where(item => item.Id == upload.Id && item.AttachedMessageId == null)
                    .ExecuteDeleteAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Could not remove expired attachment upload {UploadId}", upload.Id);
            }
        }
        return removed;
    }
}
