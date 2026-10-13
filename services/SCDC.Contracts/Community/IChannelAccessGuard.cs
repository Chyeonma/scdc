using System.Data.Common;
using SCDC.Contracts.Identity;

namespace SCDC.Contracts.Community;

public enum ChannelAccessStatus { Allowed, Hidden, InvalidSession, AccountDenied, Unavailable }

public sealed record ChannelAccessLease(ChannelAccessStatus Status, Guid? MembershipId = null,
    string? ServerAccessVersion = null, string? ChannelAccessVersion = null,
    bool CanSendText = false, DateTimeOffset? SessionExpiresAt = null)
{
    public bool IsAllowedAt(DateTimeOffset now) => Status == ChannelAccessStatus.Allowed && SessionExpiresAt > now;
}

public interface IChannelAccessGuard
{
    // Caller retains this transaction/locks and checks lease expiry immediately before commit.
    // Messaging must also check its own space state and authorship inside this transaction.
    Task<ChannelAccessLease> AcquireAsync(AccountActor actor, Guid serverId, Guid channelId,
        DbTransaction transaction, CancellationToken cancellationToken);
}
