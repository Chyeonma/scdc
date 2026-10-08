using System.Data.Common;

namespace SCDC.Contracts.Identity;

public sealed record AccountActor(Guid UserId, Guid SessionId, Guid SecurityStamp);

public enum AccountAccessStatus
{
    Allowed,
    InvalidSession,
    AccountDenied
}

public sealed record AccountAccessCheck(AccountAccessStatus Status, DateTimeOffset? SessionExpiresAt = null)
{
    public bool IsAllowedAt(DateTimeOffset now) => Status == AccountAccessStatus.Allowed && SessionExpiresAt > now;
}

public interface IAccountAccessGuard
{
    // The caller owns the transaction and must retain it until the operation finishes.
    Task<AccountAccessCheck> AcquireAsync(AccountActor actor, DbTransaction transaction, CancellationToken cancellationToken);
}
