namespace SCDC.Contracts.Identity;

public sealed record AuthenticatedSession(Guid UserId, Guid SessionId, Guid SecurityStamp, DateTimeOffset AccessExpiresAt);
public sealed record SessionAccess(bool Allowed, string? Reason = null);

/// <summary>Identity owns session/stamp/eligibility data. Read checks do not require an active peer.</summary>
public interface IAuthenticatedSessionReader
{
    Task<SessionAccess> ReadAsync(AuthenticatedSession actor, CancellationToken cancellationToken);
}
