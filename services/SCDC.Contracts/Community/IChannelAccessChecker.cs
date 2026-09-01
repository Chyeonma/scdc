namespace SCDC.Contracts.Community;

public interface IChannelAccessChecker
{
    Task<ChannelAccessDecision> CheckAsync(
        Guid userId,
        Guid spaceId,
        CancellationToken cancellationToken);
}

public sealed record ChannelAccessDecision(bool CanRead, bool CanSend)
{
    public static ChannelAccessDecision Denied { get; } = new(false, false);
}
