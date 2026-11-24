using SCDC.Contracts.Identity;

namespace SCDC.Contracts.Community;

// Kind 3: membership; 4: channel access; 5: management; 6: channel lifecycle.
public sealed record CommunityScope(short Kind, Guid UserId, Guid ServerId, Guid ChannelId,
    Guid MembershipId, string Permission, long Epoch);
public sealed record CommunityFenceBatch(Guid OperationId, Guid BatchId, long RoutingGeneration,
    IReadOnlyList<CommunityScope> Scopes);
public sealed record CommunityFenceReceipt(AccessOwner Owner, CommunityFenceBatch Batch);
/// <summary>Owner-local commit and durable, replayable receipt. Repeat the same operation/batch with
/// unchanged scopes after an unknown outcome; changed content conflicts. Callers impose a two-second deadline.</summary>
public interface ICommunityFenceOwner
{
    AccessOwner Owner { get; }
    Task<CommunityFenceReceipt> ApplyAsync(CommunityFenceBatch batch, CancellationToken cancellationToken);
}
public sealed class CommunityChangePendingException(Guid operationId, Guid serverId) : Exception("The accepted policy change is pending.")
{
    public Guid OperationId { get; } = operationId;
    public Guid ServerId { get; } = serverId;
}
public sealed record CommunityChangeStatus(Guid OperationId, string Phase, string Version, bool CleanupPending, string? ErrorCode);
public interface ICommunityChangeStatusReader
{
    Task<CommunityChangeStatus?> ReadAsync(Guid operationId, AccountActor actor, Guid serverId, CancellationToken cancellationToken);
}
