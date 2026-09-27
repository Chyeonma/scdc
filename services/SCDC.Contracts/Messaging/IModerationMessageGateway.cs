namespace SCDC.Contracts.Messaging;

public interface IModerationMessageGateway
{
    Task<ModerationMessageEvidence?> GetReportableAsync(Guid userId, Guid spaceId, Guid messageId, CancellationToken cancellationToken);
    Task<ModerationReviewAccess?> GetReviewAccessAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);
    Task<bool> RemoveAsync(Guid userId, Guid spaceId, Guid messageId, CancellationToken cancellationToken);
}

public sealed record ModerationMessageEvidence(Guid MessageId, Guid SpaceId, Guid? AuthorUserId,
    short MessageType, string? Content, DateTimeOffset CreatedAt, int Version);

public sealed record ModerationReviewAccess(Guid SpaceId, Guid? ServerId);
