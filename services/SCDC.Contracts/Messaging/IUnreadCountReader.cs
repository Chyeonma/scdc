namespace SCDC.Contracts.Messaging;

public interface IUnreadCountReader
{
    Task<IReadOnlyDictionary<Guid, SpaceUnreadState>> GetAsync(
        Guid userId,
        IReadOnlyCollection<Guid> spaceIds,
        CancellationToken cancellationToken);
}

public sealed record SpaceUnreadState(int UnreadCount, string? LastReadSequence);
