using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;

namespace SCDC.Community.Api.Preparation;

// Missing integrations must never create a space, acknowledge a fence, or report success.
internal sealed class PendingMessaging : IChannelProvisioning, ICommunityFenceOwner
{
    public AccessOwner Owner => AccessOwner.Messaging;
    private static Task<T> Unavailable<T>() => Task.FromException<T>(
        new AccessCheckException(CommunityPreparation.ErrorCode));
    public Task<ProvisionStatus> PrepareAsync(PrepareChannel request, CancellationToken ct) => Unavailable<ProvisionStatus>();
    public Task<ProvisionStatus> ConfirmAsync(ChannelDecision request, CancellationToken ct) => Unavailable<ProvisionStatus>();
    public Task<ProvisionStatus> CancelAsync(ChannelDecision request, CancellationToken ct) => Unavailable<ProvisionStatus>();
    public Task<ProvisionStatus?> GetAsync(Guid operationId, Guid channelId, CancellationToken ct) => Unavailable<ProvisionStatus?>();
    public Task<ProvisionStatus> ActivateAsync(ChannelReadyPermit permit, CancellationToken ct) => Unavailable<ProvisionStatus>();
    public Task<CommunityFenceReceipt> ApplyAsync(CommunityFenceBatch batch, CancellationToken ct) => Unavailable<CommunityFenceReceipt>();
}
