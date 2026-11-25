using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SCDC.BuildingBlocks.Infrastructure.Transport;
using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;
using M = SCDC.Messaging.Grpc.V1;

namespace SCDC.Modules.Community.Infrastructure.Transport;

public static class RemoteMessagingRegistration
{
    public static IServiceCollection AddRemoteMessaging(this IServiceCollection services)
    {
        services.AddKeyedSingleton<IChannelProvisioning, GrpcChannelProvisioning>(AccessOwner.Community);
        services.AddSingleton<ICommunityFenceOwner, GrpcCommunityFence>();
        services.AddSingleton<CommunityDependencyReadiness>();
        return services;
    }
    internal static Grpc.Net.Client.GrpcChannel Channel(RpcTransport transport, IConfiguration configuration) =>
        transport.Channel(configuration["InternalRpc:Messaging:Endpoint"] ?? throw new InvalidOperationException("Messaging RPC endpoint is required."), "chat-service");
}

public sealed class GrpcChannelProvisioning(RpcTransport transport, IConfiguration configuration) : IChannelProvisioning
{
    private M.ChannelProvisioning.ChannelProvisioningClient Client => new(RemoteMessagingRegistration.Channel(transport, configuration));
    private static ProvisionStatus? Status(M.ProvisionStatus wire, Guid operation, Guid channel, bool optional = false) => ServiceRpc.Decode(() =>
    {
        var value = CommunityWire.Read(wire);
        if (value is null ? !optional : value.OperationId != operation || value.ChannelId != channel) throw new FormatException();
        return value;
    });
    public async Task<ProvisionStatus> PrepareAsync(PrepareChannel request, CancellationToken ct) => Status(await ServiceRpc.ReadAsync(
        Client.PrepareAsync(CommunityWire.Write(request), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: ct), ct), request.OperationId, request.ChannelId)!;
    public async Task<ProvisionStatus> ConfirmAsync(ChannelDecision request, CancellationToken ct) => Status(await ServiceRpc.ReadAsync(
        Client.ConfirmAsync(CommunityWire.Write(request), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: ct), ct), request.OperationId, request.ChannelId)!;
    public async Task<ProvisionStatus> CancelAsync(ChannelDecision request, CancellationToken ct) => Status(await ServiceRpc.ReadAsync(
        Client.CancelAsync(CommunityWire.Write(request), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: ct), ct), request.OperationId, request.ChannelId)!;
    public async Task<ProvisionStatus?> GetAsync(Guid operationId, Guid channelId, CancellationToken ct) => Status(await ServiceRpc.ReadAsync(
        Client.GetAsync(new M.ProvisionQuery { OperationId = operationId.ToString("D"), ChannelId = channelId.ToString("D"), RoutingGeneration = 1 },
            deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: ct), ct), operationId, channelId, optional: true);
    public async Task<ProvisionStatus> ActivateAsync(ChannelReadyPermit permit, CancellationToken ct) => Status(await ServiceRpc.ReadAsync(
        Client.ActivateAsync(CommunityWire.Write(permit), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: ct), ct), permit.OperationId, permit.ChannelId)!;
}

public sealed class GrpcCommunityFence(RpcTransport transport, IConfiguration configuration) : ICommunityFenceOwner
{
    public AccessOwner Owner => AccessOwner.Messaging;
    public async Task<CommunityFenceReceipt> ApplyAsync(CommunityFenceBatch batch, CancellationToken ct)
    {
        var client = new M.CommunityPolicyFence.CommunityPolicyFenceClient(RemoteMessagingRegistration.Channel(transport, configuration));
        var reply = await ServiceRpc.ReadAsync(client.ApplyAsync(CommunityWire.Write(batch), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: ct), ct);
        return ServiceRpc.Decode(() =>
        {
            var value = CommunityWire.Read(reply);
            if (value.Owner != Owner || System.Text.Json.JsonSerializer.Serialize(value.Batch) != System.Text.Json.JsonSerializer.Serialize(batch)) throw new FormatException();
            return value;
        });
    }
}

public sealed class CommunityDependencyReadiness(RpcTransport transport, IConfiguration configuration)
{
    public async Task<bool> IsReadyAsync(CancellationToken ct)
    {
        try
        {
            var identity = new SCDC.Identity.Grpc.V1.IdentityAccess.IdentityAccessClient(transport.Channel(
                configuration["InternalRpc:Identity:Endpoint"] ?? throw new InvalidOperationException("Identity RPC endpoint is required."), "identity-service"));
            var messaging = new M.MessagingControl.MessagingControlClient(RemoteMessagingRegistration.Channel(transport, configuration));
            var probes = await Task.WhenAll(
                ServiceRpc.ReadAsync(identity.ProbeAsync(new Empty(), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: ct), ct),
                ServiceRpc.ReadAsync(messaging.ProbeAsync(new Empty(), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: ct), ct));
            return probes.All(result => result.Ready);
        }
        catch (Exception error) when (error is AccessCheckException or OperationCanceledException or TimeoutException) { return false; }
    }
}
