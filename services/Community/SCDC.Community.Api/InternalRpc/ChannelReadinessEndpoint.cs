using Grpc.Core;
using SCDC.Modules.Community.Infrastructure.Transport;
using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;
using C = SCDC.Community.Grpc.V1;

namespace SCDC.Community.Api.InternalRpc;

public sealed class ChannelReadinessEndpoint(IChannelReadyPermitIssuer permits) : C.ChannelReadiness.ChannelReadinessBase
{
    public override async Task<C.ChannelReadyPermit> Acquire(C.ChannelReadyRequest request, ServerCallContext context)
    {
        RpcPeerGuard.RequirePeer(context, "chat-service");
        var wire = request.Identity ?? throw new FormatException();
        var value = IdentityWire.Read(wire);
        var server = Guid.Parse(request.ServerId); var channel = Guid.Parse(request.ChannelId);
        if (wire.Owner != SCDC.Identity.Grpc.V1.Owner.Messaging || value.Purpose != IdentityPurpose.ChannelRead
            || value.ResourceId != channel || value.PeerId is not null || value.Context != IdentityAccessContext.HttpCaller
            || value.RoutingGeneration != 1 || server == Guid.Empty || channel == Guid.Empty) throw new FormatException();
        var actor = new AccountActor(value.Actor.UserId, value.Actor.SessionId, value.Actor.SecurityStamp, value.Actor.AccessExpiresAt,
            request.MembershipId.Length == 0 ? null : Guid.Parse(request.MembershipId));
        return CommunityWire.Write(await permits.AcquireAsync(actor, server, channel, context.CancellationToken));
    }
}
