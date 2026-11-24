namespace SCDC.Contracts.Identity;

public sealed record AccountActor(Guid UserId, Guid SessionId, Guid SecurityStamp,
    DateTimeOffset AccessExpiresAt = default, Guid? ExpectedMembershipId = null);
