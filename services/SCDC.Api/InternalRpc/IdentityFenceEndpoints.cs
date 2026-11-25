using Grpc.Core;
using SCDC.BuildingBlocks.Infrastructure.Transport;
using SCDC.Contracts.Identity;
using W = SCDC.Identity.Grpc.V1;

namespace SCDC.Api.InternalRpc;

public sealed class MessagingIdentityFenceEndpoint(IEnumerable<IIdentityFenceOwner> owners)
    : SCDC.Messaging.Grpc.V1.MessagingFence.MessagingFenceBase
{
    public override async Task<W.FenceReceipt> Apply(W.FenceBatch request, ServerCallContext context)
    {
        RpcPeerGuard.RequirePeer(context, "identity-service");
        return IdentityWire.Write(await owners.Single(owner => owner.Owner == AccessOwner.Messaging).ApplyAsync(IdentityWire.Read(request), context.CancellationToken));
    }
}
