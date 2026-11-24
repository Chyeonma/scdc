namespace SCDC.Contracts.Identity;

/// <summary>Fixed public codes only; never wrap provider exceptions or credentials.</summary>
public sealed class AccessCheckException(string code = "ACCESS_CHECK_UNAVAILABLE", Guid? operationId = null)
    : Exception("Access could not be confirmed.")
{
    public string Code { get; } = code;
    public Guid? OperationId { get; } = operationId;
}

public enum AccessOwner { Community = 1, Messaging = 2 }
public enum IdentityPurpose
{
    AccountRead = 1, CommunityWrite, DmOpenNew, DmRead, DmSendNew, DmReplay,
    DmEditOwn, DmDeleteOwn, ChannelRead, ChannelSend, ChannelReplay, ChannelEditOwn,
    ChannelDeleteOwn, ChannelCreate, CommunityManage = 17
}
public sealed record IdentityScope(short Kind, Guid UserId, Guid SessionId, long Epoch);
public enum IdentityAccessContext { HttpCaller = 1, ResumingAcceptedOperation = 2 }
public sealed record PermitRequest(AuthenticatedSession Actor, IdentityPurpose Purpose, Guid ResourceId,
    Guid OperationId, Guid? PeerId = null, long RoutingGeneration = 1,
    IdentityAccessContext Context = IdentityAccessContext.HttpCaller);
public sealed record IdentityPermit(Guid Id, AccessOwner Owner, PermitRequest Request,
    IReadOnlyList<IdentityScope> Scopes, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt);

/// <summary>Resolve by trusted owner key; callers impose a two-second deadline before opening their
/// local transaction. Reacquisition revalidates access and can issue a new short-lived permit;
/// it must not advance epochs or authorize an operation already rejected by a local fence.</summary>
public interface IIdentityPermitIssuer
{
    Task<IdentityPermit> AcquireAsync(PermitRequest request, CancellationToken cancellationToken);
}
public sealed record IdentityFenceBatch(Guid OperationId, Guid BatchId, long RoutingGeneration,
    IReadOnlyList<IdentityScope> Scopes);
public sealed record IdentityFenceReceipt(AccessOwner Owner, IdentityFenceBatch Batch);
/// <summary>Apply under the owner's transaction and acknowledge only after commit. Repeating the same
/// operation/batch and scopes is safe; conflicting content is rejected and epochs never decrease.
/// The caller uses a two-second deadline; a timeout leaves the outcome unknown and is retried with the same IDs.</summary>
public interface IIdentityFenceOwner
{
    AccessOwner Owner { get; }
    Task<IdentityFenceReceipt> ApplyAsync(IdentityFenceBatch batch, CancellationToken cancellationToken);
}
public sealed record RevocationStatus(Guid OperationId, string Phase, long Version, bool CleanupPending);
public interface IRevocationStatusReader
{
    Task<RevocationStatus?> ReadAsync(Guid operationId, Guid userId, CancellationToken cancellationToken);
}
