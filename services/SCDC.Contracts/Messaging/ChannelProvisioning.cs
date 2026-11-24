namespace SCDC.Contracts.Messaging;

// Service-only contract. The in-process adapter is registered for Community;
// no HTTP caller can supply the producer identity or an owner transaction.
public sealed record PrepareChannel(Guid OperationId, Guid ChannelId, Guid ServerId, Guid ActorId,
    DateTimeOffset CreatedAt, byte[] Fingerprint, long RoutingGeneration = 1, long ChannelEpoch = 1);
public sealed record ChannelDecision(Guid OperationId, Guid ChannelId, byte[] Fingerprint,
    long DecisionVersion = 1, long RoutingGeneration = 1, long ChannelEpoch = 1);
public enum ProvisionPhase { Prepared, Ready, Cancelled }
public sealed record ProvisionStatus(Guid OperationId, Guid ChannelId, ProvisionPhase Phase,
    long DecisionVersion, bool CleanupPending = false);
public interface IChannelProvisioning
{
    Task<ProvisionStatus> PrepareAsync(PrepareChannel request, CancellationToken ct);
    Task<ProvisionStatus> ConfirmAsync(ChannelDecision request, CancellationToken ct);
    Task<ProvisionStatus> CancelAsync(ChannelDecision request, CancellationToken ct);
    Task<ProvisionStatus?> GetAsync(Guid operationId, Guid channelId, CancellationToken ct);
    Task<ProvisionStatus> ActivateAsync(SCDC.Contracts.Community.ChannelReadyPermit permit, CancellationToken ct);
}
public sealed class ProvisionConflictException(string message) : Exception(message);
public sealed class ChannelOutcomeUnknownException(Guid serverId, Guid clientOperationId) : Exception("The channel creation outcome must be checked.")
{
    public Guid ServerId { get; } = serverId;
    public Guid ClientOperationId { get; } = clientOperationId;
}
