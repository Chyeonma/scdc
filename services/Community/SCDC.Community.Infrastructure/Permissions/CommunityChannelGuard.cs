using SCDC.Modules.Community.Infrastructure.Access;
using System.Globalization;
using Npgsql;
using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Modules.Community.Infrastructure.Persistence;
using SCDC.Modules.Community.Features.Permissions.Application;
using SCDC.Modules.Community.Features.Permissions.Domain;

namespace SCDC.Modules.Community.Features.Permissions.Infrastructure;

internal sealed record ChannelLease(ManagementLease Server, ChannelState Channel, PermissionEvaluation Evaluation);
internal sealed class CommunityChannelGuard(CommunityManagementGuard management, ChannelReader reader)
{
    public async Task<ChannelLease> AcquireCoreAsync(RelationalWorkScope scope, AccountActor actor, Guid server, Guid channel,
        bool write, string? permission, CancellationToken ct, IdentityPermit permit, Guid? acceptedOperation = null)
    {
        var lease = await management.AcquireAsync(scope, actor, server, write, false, ct, permit, requiredPermission: null, acceptedOperation: acceptedOperation);
        var state = await reader.StateAsync(scope, server, channel, write, ct) ?? throw PermissionFailure.Missing();
        var evaluation = await reader.EvaluateAsync(scope, actor.UserId, server, state, lease, ct);
        if (!evaluation.CanView) throw PermissionFailure.Missing();
        if (permission is not null && !evaluation.ManagementPermissions.Contains(permission))
            throw new PermissionFailure(Error.Forbidden("PERMISSION_DENIED", "The required channel management permission is missing."));
        var required=new List<CommunityScope>{CommunityScopeStore.Channel(actor.UserId,server,channel,lease.MembershipId),CommunityScopeStore.Lifecycle(server,channel)};
        if(permission is not null)required.Add(CommunityScopeStore.Management(actor.UserId,server,lease.MembershipId,permission));
        await CommunityScopeStore.CheckAsync(scope,required,acceptedOperation,ct);
        return new(lease, state, evaluation);
    }
}
