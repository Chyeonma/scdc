namespace SCDC.Contracts.Community;

public interface IChannelAccessChecker
{
    Task<ChannelAccessDecision> CheckAsync(
        Guid userId,
        Guid spaceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ListReadableMemberIdsAsync(Guid spaceId, CancellationToken cancellationToken);
}

public sealed record ChannelAccessDecision(bool CanRead, bool CanSend, bool CanEditOwn = false, bool CanDeleteOthers = false)
{
    public static ChannelAccessDecision Denied { get; } = new(false, false);
}
