namespace SCDC.Contracts.Messaging;

public interface IPlatformReviewAccess
{
    Task<bool> IsReviewerAsync(Guid userId, CancellationToken cancellationToken);
}
