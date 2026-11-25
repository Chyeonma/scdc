using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SCDC.Contracts.Identity;
using SCDC.Modules.Identity.Infrastructure.Transport;

namespace SCDC.Modules.Identity;

public static class RemoteFenceRegistration
{
    public static IServiceCollection AddRemoteFenceOwners(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<RpcTransport>();
        foreach (var owner in new[] { AccessOwner.Community, AccessOwner.Messaging })
            services.AddSingleton<IIdentityFenceOwner>(provider => new GrpcFenceOwner(provider.GetRequiredService<RpcTransport>(), configuration, owner));
        return services;
    }
}

internal sealed class GrpcFenceOwner(RpcTransport transport, IConfiguration configuration, AccessOwner owner) : IIdentityFenceOwner
{
    public AccessOwner Owner => owner;
    public async Task<IdentityFenceReceipt> ApplyAsync(IdentityFenceBatch batch, CancellationToken ct)
    {
        var endpoint = configuration[$"InternalRpc:{owner}:Endpoint"]
            ?? throw new InvalidOperationException("Fence owner RPC endpoint is required.");
        var channel = transport.Channel(endpoint, "chat-service");
        using var call = owner == AccessOwner.Community
            ? new SCDC.Community.Grpc.V1.CommunityFence.CommunityFenceClient(channel).ApplyAsync(IdentityWire.Write(batch), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: ct)
            : new SCDC.Messaging.Grpc.V1.MessagingFence.MessagingFenceClient(channel).ApplyAsync(IdentityWire.Write(batch), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: ct);
        try { return IdentityWire.Read(await call.ResponseAsync.WaitAsync(ct)); }
        catch (Exception error) when (error is RpcException or FormatException or ArgumentException or OverflowException) { throw new AccessCheckException(); }
    }
}
