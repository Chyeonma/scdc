using Grpc.Core;
using SCDC.Contracts.Identity;
using SCDC.Modules.Identity.Infrastructure.Transport;
using W = SCDC.Identity.Grpc.V1;
using Google.Protobuf.WellKnownTypes;
using SCDC.Modules.Identity.Application;

namespace SCDC.Identity.Api.InternalRpc;

public sealed class IdentityAccessEndpoint(IServiceProvider services, IConfiguration configuration) : W.IdentityAccess.IdentityAccessBase
{
    public override async Task<W.ServiceReadiness> Probe(Empty request, ServerCallContext context) =>
        new() { Ready = await services.GetRequiredService<IIdentityReadiness>().IsReadyAsync(context.CancellationToken) };

    public override async Task<W.AccessReply> AcquirePermit(W.AccessRequest request, ServerCallContext context)
    {
        var owner = (AccessOwner)(int)request.Owner;
        if (!System.Enum.IsDefined(owner)) throw new AccessCheckException("INVALID_PERMIT_REQUEST");
        var peer = context.GetHttpContext().Connection.ClientCertificate!
            .GetNameInfo(System.Security.Cryptography.X509Certificates.X509NameType.DnsName, false);
        var owners = configuration.GetSection("InternalRpc:PeerOwners:" + peer).Get<string[]>() ?? [];
        // Delegation is purpose-specific, never an additional owner-wide permission.
        var delegated = configuration.GetSection($"InternalRpc:PermitDelegations:{peer}:{owner}").Get<string[]>() ?? [];
        if (!owners.Contains(owner.ToString(), StringComparer.Ordinal)
            && !(owner == AccessOwner.Messaging && (IdentityPurpose)(int)request.Purpose == IdentityPurpose.ChannelRead
                && delegated.Contains(nameof(IdentityPurpose.ChannelRead), StringComparer.Ordinal)))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Peer cannot request permits for this owner."));
        var permit = await services.GetRequiredKeyedService<IIdentityPermitIssuer>(owner).AcquireAsync(IdentityWire.Read(request), context.CancellationToken);
        return new W.AccessReply { Permit = IdentityWire.Write(permit) };
    }
}

public sealed class IdentityDirectoryEndpoint(IUserDirectory users, IHistoricalUserSummaryReader history) : W.IdentityDirectory.IdentityDirectoryBase
{
    private static Guid[] Ids(W.ProfileQuery query)
    {
        if (query.UserIds.Count > 100 || query.Username.Length > 32 || (query.UserIds.Count > 0 && query.Username.Length > 0)) throw new FormatException();
        var ids = query.UserIds.Select(Guid.Parse).ToArray();
        if (ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length) throw new FormatException();
        return ids;
    }
    private static W.ProfileList Reply(IEnumerable<UserSummary> profiles)
    {
        var reply = new W.ProfileList();
        reply.Items.AddRange(profiles.Select(profile => new W.Profile { UserId = profile.Id.ToString("D"), Username = profile.Username, DisplayName = profile.DisplayName, Availability = profile.Availability ?? "" }));
        return reply;
    }
    public override async Task<W.ProfileList> LookupProfiles(W.ProfileQuery request, ServerCallContext context)
    {
        var ids = Ids(request);
        if (request.Username.Length > 0)
        {
            var user = await users.FindByUsernameAsync(request.Username, context.CancellationToken);
            return Reply(user is null ? [] : [user]);
        }
        return Reply((await users.FindByIdsAsync(ids, context.CancellationToken)).Values);
    }
    public override async Task<W.ProfileList> LookupHistorical(W.ProfileQuery request, ServerCallContext context)
    {
        var ids = Ids(request);
        if (request.Username.Length != 0) throw new FormatException();
        return Reply((await history.ReadAsync(ids, context.CancellationToken)).Values);
    }
}
