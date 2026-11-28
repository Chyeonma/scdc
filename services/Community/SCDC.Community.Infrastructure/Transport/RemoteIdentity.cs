using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SCDC.Contracts.Identity;
using W = SCDC.Identity.Grpc.V1;

namespace SCDC.Modules.Community.Infrastructure.Transport;

public static class RemoteIdentityRegistration
{
    public static IServiceCollection AddRemoteIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<RpcTransport>();
        foreach (var owner in new[] { AccessOwner.Community, AccessOwner.Messaging })
            services.AddKeyedScoped<IIdentityPermitIssuer>(owner, (provider, _) =>
                new GrpcIdentityPermits(provider.GetRequiredService<RpcTransport>(), configuration, owner));
        services.AddScoped<IUserDirectory, GrpcIdentityDirectory>();
        services.AddScoped<IHistoricalUserSummaryReader, GrpcIdentityDirectory>();
        return services;
    }
}

internal static class IdentityRpc
{
    public static W.IdentityAccess.IdentityAccessClient Access(RpcTransport transport, IConfiguration configuration) => new(Channel(transport, configuration));
    public static Grpc.Net.Client.GrpcChannel Channel(RpcTransport transport, IConfiguration configuration) => transport.Channel(
        configuration["InternalRpc:Identity:Endpoint"] ?? throw new InvalidOperationException("Identity RPC endpoint is required."), "identity-service");
    public static async Task<T> Run<T>(AsyncUnaryCall<T> call, CancellationToken ct)
    {
        using (call)
        {
            try { return await call.ResponseAsync.WaitAsync(ct); }
            catch (RpcException error)
            {
                var code = error.StatusCode == StatusCode.FailedPrecondition
                    ? RpcPeerGuard.PublicCode(error.Trailers.FirstOrDefault(item => item.Key == "scdc-error-code")?.Value) : "ACCESS_CHECK_UNAVAILABLE";
                throw new AccessCheckException(code);
            }
        }
    }
}

public sealed class GrpcIdentityPermits(RpcTransport transport, IConfiguration configuration, AccessOwner owner) : IIdentityPermitIssuer
{
    public async Task<IdentityPermit> AcquireAsync(PermitRequest request, CancellationToken ct)
    {
        var reply = await IdentityRpc.Run(IdentityRpc.Access(transport, configuration).AcquirePermitAsync(IdentityWire.Write(request, owner),
            deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: ct), ct);
        try
        {
            if (reply.Permit is null) throw new AccessCheckException();
            var permit = IdentityWire.Read(reply.Permit);
            if (permit.Owner != owner || permit.Request != request) throw new AccessCheckException();
            return permit;
        }
        catch (Exception error) when (error is FormatException or ArgumentException or OverflowException) { throw new AccessCheckException(); }
    }
}

public sealed class GrpcIdentityDirectory(RpcTransport transport, IConfiguration configuration) : IUserDirectory, IHistoricalUserSummaryReader
{
    private async Task<IReadOnlyDictionary<Guid,UserSummary>> Lookup(IReadOnlyCollection<Guid> ids, string username, bool historical, CancellationToken ct)
    {
        if (ids.Count == 0 && username.Length == 0) return new Dictionary<Guid,UserSummary>();
        var query = new W.ProfileQuery { Username = username }; query.UserIds.AddRange(ids.Select(id => id.ToString("D")));
        var client = new W.IdentityDirectory.IdentityDirectoryClient(IdentityRpc.Channel(transport, configuration));
        var reply = await IdentityRpc.Run(historical
            ? client.LookupHistoricalAsync(query, deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: ct)
            : client.LookupProfilesAsync(query, deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: ct), ct);
        try
        {
            return reply.Items.ToDictionary(item => Guid.Parse(item.UserId), item =>
                new UserSummary(Guid.Parse(item.UserId), item.Username, item.DisplayName, item.Availability.Length == 0 ? null : item.Availability));
        }
        catch (Exception error) when (error is FormatException or ArgumentException) { throw new AccessCheckException(); }
    }
    public async Task<UserSummary?> FindByIdAsync(Guid userId, CancellationToken ct) => (await Lookup([userId], "", false, ct)).GetValueOrDefault(userId);
    public async Task<UserSummary?> FindByUsernameAsync(string username, CancellationToken ct) => (await Lookup([], username, false, ct)).Values.SingleOrDefault();
    public Task<IReadOnlyDictionary<Guid,UserSummary>> FindByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) => Lookup(ids, "", false, ct);
    public Task<IReadOnlyDictionary<Guid,UserSummary>> ReadAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) => Lookup(ids, "", true, ct);
}
