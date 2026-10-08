namespace SCDC.Contracts.Identity;

public sealed record AccountAccessRequest(Guid ActorId, Guid SessionId, Guid SecurityStamp, Guid PeerId);
public sealed record AccountAccessFailure(string Code);
public sealed record AccountAccessResult(UserSummary? Actor, UserSummary? Peer, bool PeerEligible, AccountAccessFailure? Failure);

/// <summary>Enlists in the shared transaction and holds ordered user/session SHARE locks until owner completion.</summary>
public interface IAccountAccessGuard
{
    Task<AccountAccessResult> LockPairAsync(AccountAccessRequest request, CancellationToken cancellationToken);
    Task DetachAsync(CancellationToken cancellationToken);
}

/// <summary>Internal public projection for callers that have already acquired the appropriate access guard.</summary>
public interface IHistoricalUserSummaryReader
{
    Task<IReadOnlyDictionary<Guid, UserSummary>> ReadAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
