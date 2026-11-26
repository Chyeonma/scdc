using Grpc.Core;
using SCDC.BuildingBlocks.Infrastructure.Transport;
using SCDC.Contracts.Identity;
using W = SCDC.Identity.Grpc.V1;

namespace SCDC.Api.InternalRpc;

public sealed class CommunityIdentityFenceEndpoint(IEnumerable<IIdentityFenceOwner> owners)
    : SCDC.Community.Grpc.V1.CommunityFence.CommunityFenceBase
{
    public override async Task<W.FenceReceipt> Apply(W.FenceBatch request, ServerCallContext context)
    {
        RpcPeerGuard.RequirePeer(context, "identity-service");
        return IdentityWire.Write(await owners.Single(owner => owner.Owner == AccessOwner.Community)
            .ApplyAsync(IdentityWire.Read(request), context.CancellationToken));
    }
}
