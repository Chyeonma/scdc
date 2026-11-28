using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Npgsql;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.BuildingBlocks.Infrastructure.Transport;
using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;
using SCDC.Messaging.Grpc;
using C = SCDC.Community.Grpc.V1;
using M = SCDC.Messaging.Grpc.V1;

namespace SCDC.Api.InternalRpc;

public sealed class MessagingControlEndpoint(IConfiguration configuration) : M.MessagingControl.MessagingControlBase
{
    public override async Task<SCDC.Identity.Grpc.V1.ServiceReadiness> Probe(Empty request, ServerCallContext context)
    {
        RpcPeerGuard.RequirePeer(context, "community-service");
        await using var connection = new NpgsqlConnection(ModuleDatabaseConnection.Get(configuration, "Messaging"));
        await connection.OpenAsync(context.CancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT to_regclass('messaging.channel_provisions') IS NOT NULL
                AND to_regclass('messaging.access_fences') IS NOT NULL
                AND to_regclass('messaging.fence_receipts') IS NOT NULL
            """, connection);
        return new() { Ready = await command.ExecuteScalarAsync(context.CancellationToken) is true };
    }
}

public sealed class MessagingCommunityFenceEndpoint(IEnumerable<ICommunityFenceOwner> owners)
    : M.CommunityPolicyFence.CommunityPolicyFenceBase
{
    public override async Task<C.CommunityFenceReceipt> Apply(C.CommunityFenceBatch request, ServerCallContext context)
    {
        RpcPeerGuard.RequirePeer(context, "community-service");
        var receipt = await owners.Single(owner => owner.Owner == AccessOwner.Messaging)
            .ApplyAsync(CommunityWire.Read(request), context.CancellationToken);
        return CommunityWire.Write(receipt);
    }
}

public sealed class MessagingChannelProvisionEndpoint(
    [FromKeyedServices(AccessOwner.Community)] IChannelProvisioning provision) : M.ChannelProvisioning.ChannelProvisioningBase
{
    private static async Task<M.ProvisionStatus> Run(ServerCallContext context, Func<Task<ProvisionStatus?>> action)
    {
        RpcPeerGuard.RequirePeer(context, "community-service");
        try { return ChannelProvisioningWire.Write(await action()); }
        catch (ProvisionConflictException)
        { throw new RpcException(new Status(StatusCode.AlreadyExists, "The operation conflicts with existing channel state.")); }
    }
    public override Task<M.ProvisionStatus> Prepare(M.PrepareChannel request, ServerCallContext context) =>
        Run(context, async () => await provision.PrepareAsync(ChannelProvisioningWire.Read(request), context.CancellationToken));
    public override Task<M.ProvisionStatus> Confirm(M.ChannelDecision request, ServerCallContext context) =>
        Run(context, async () => await provision.ConfirmAsync(ChannelProvisioningWire.Read(request), context.CancellationToken));
    public override Task<M.ProvisionStatus> Cancel(M.ChannelDecision request, ServerCallContext context) =>
        Run(context, async () => await provision.CancelAsync(ChannelProvisioningWire.Read(request), context.CancellationToken));
    public override Task<M.ProvisionStatus> Activate(C.ChannelReadyPermit request, ServerCallContext context) =>
        Run(context, async () => await provision.ActivateAsync(CommunityWire.Read(request), context.CancellationToken));
    public override Task<M.ProvisionStatus> Get(M.ProvisionQuery request, ServerCallContext context) => Run(context, async () =>
    {
        if (request.RoutingGeneration != 1) throw new FormatException();
        var operation = Guid.Parse(request.OperationId); var channel = Guid.Parse(request.ChannelId);
        if (operation == Guid.Empty || channel == Guid.Empty) throw new FormatException();
        return await provision.GetAsync(operation, channel, context.CancellationToken);
    });
}
