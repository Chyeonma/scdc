namespace SCDC.Modules.Messaging.Application;

public interface IAttachmentCleanupService
{
    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken);
}
