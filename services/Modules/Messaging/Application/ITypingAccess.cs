namespace SCDC.Modules.Messaging.Application;

public interface ITypingAccess
{
    Task<bool> CanReadAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);
    Task<bool> CanSendAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);
}
