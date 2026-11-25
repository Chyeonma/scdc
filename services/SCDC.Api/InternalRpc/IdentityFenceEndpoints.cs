using Grpc.Core;
using SCDC.BuildingBlocks.Infrastructure.Transport;
using SCDC.Contracts.Identity;
using W = SCDC.Identity.Grpc.V1;

namespace SCDC.Api.InternalRpc;

public sealed class CommunityIdentityFenceEndpoint(IEnumerable<IIdentityFenceOwner> owners)
    : SCDC.Community.Grpc.V1.CommunityFence.CommunityFenceBase
{
    public override async Task<W.FenceReceipt> Apply(W.FenceBatch request, ServerCallContext context) =>
        IdentityWire.Write(await owners.Single(owner => owner.Owner == AccessOwner.Community).ApplyAsync(IdentityWire.Read(request), context.CancellationToken));
}
public sealed class MessagingIdentityFenceEndpoint(IEnumerable<IIdentityFenceOwner> owners)
    : SCDC.Messaging.Grpc.V1.MessagingFence.MessagingFenceBase
{
    public override async Task<W.FenceReceipt> Apply(W.FenceBatch request, ServerCallContext context) =>
        IdentityWire.Write(await owners.Single(owner => owner.Owner == AccessOwner.Messaging).ApplyAsync(IdentityWire.Read(request), context.CancellationToken));
}
