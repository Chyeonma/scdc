using SCDC.Contracts.Identity;

namespace SCDC.Contracts.Community;

// Trusted Community proof for activating a previously prepared text space.
// It does not replace authorization on each subsequent read/send/subscription.
public sealed record ChannelReadyPermit(Guid OperationId, Guid ChannelId, Guid ServerId, byte[] Fingerprint,
    IdentityPermit Identity, IReadOnlyList<CommunityScope> Scopes, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt);
public interface IChannelReadyPermitIssuer
{
    Task<ChannelReadyPermit> AcquireAsync(AccountActor actor, Guid server, Guid channel, CancellationToken ct);
}
