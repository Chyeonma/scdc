using Microsoft.Extensions.DependencyInjection;
using SCDC.Modules.Community.Infrastructure.Persistence;
using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Features.Permissions.Application;
using SCDC.Modules.Community.Features.Permissions.Infrastructure;
using SCDC.Modules.Community.Infrastructure.Access;

namespace SCDC.Modules.Community.Infrastructure;

internal sealed class ChannelReadyPermitIssuer(RelationalWorkScopeFactory scopes,
    CommunityManagementGuard management, CommunityChannelGuard guard,
    [FromKeyedServices(AccessOwner.Messaging)] IIdentityPermitIssuer identities, TimeProvider clock) : IChannelReadyPermitIssuer
{
    public async Task<ChannelReadyPermit> AcquireAsync(AccountActor actor, Guid server, Guid channel, CancellationToken ct)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct); deadline.CancelAfter(TimeSpan.FromSeconds(2));
        var request = new PermitRequest(new(actor.UserId, actor.SessionId, actor.SecurityStamp, actor.AccessExpiresAt),
            IdentityPurpose.ChannelRead, channel, Guid.NewGuid());
        var identity = await identities.AcquireAsync(request, deadline.Token).WaitAsync(deadline.Token);
        if (identity.Request != request || identity.Owner != AccessOwner.Messaging) throw new AccessCheckException("ACCESS_CHANGED");
        var permit = await management.PrepareAsync(actor, server, false, ct);
        await using var local = await scopes.OpenAsync(ct);
        var lease = await guard.AcquireCoreAsync(local, actor, server, channel, false, null, ct, permit);
        if (lease.Channel.View.Kind != "text") throw PermissionFailure.Missing();
        Guid operation; byte[] fingerprint;
        await using (var query = local.CreateCommand("SELECT operation_id,fingerprint FROM community.channel_provisions WHERE channel_id=@channel AND server_id=@server AND phase=1 FOR SHARE"))
        {
            query.Parameters.AddWithValue("channel", channel); query.Parameters.AddWithValue("server", server);
            await using var rows = await query.ExecuteReaderAsync(ct);
            if (!await rows.ReadAsync(ct)) throw PermissionFailure.Missing();
            operation = rows.GetGuid(0); fingerprint = rows.GetFieldValue<byte[]>(1);
        }
        var stamps = new List<CommunityScope>();
        foreach (var scope in new[] { CommunityScopeStore.Membership(actor.UserId, server, lease.Server.MembershipId),
            CommunityScopeStore.Channel(actor.UserId, server, channel, lease.Server.MembershipId), CommunityScopeStore.Lifecycle(server, channel) })
        {
            await using var query = local.CreateCommand("SELECT current_epoch FROM community.access_scopes WHERE " + CommunityScopeStore.Match + " FOR SHARE");
            CommunityScopeStore.Bind(query, scope);
            stamps.Add(scope with { Epoch = (long)(await query.ExecuteScalarAsync(ct))! });
        }
        var now = clock.GetUtcNow(); var expires = new[] { identity.ExpiresAt, permit.ExpiresAt, now.AddSeconds(5) }.Min();
        if (expires - now < TimeSpan.FromMilliseconds(1500)) throw new AccessCheckException("ACCESS_CHANGED");
        await management.EnsureAsync(local, lease.Server.Account, ct); await local.CommitAsync(ct);
        return new(operation, channel, server, fingerprint, identity, stamps, now, expires);
    }
}
