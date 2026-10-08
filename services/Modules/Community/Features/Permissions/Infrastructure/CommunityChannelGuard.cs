using System.Data.Common;
using System.Globalization;
using Npgsql;
using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Modules.Community.Features.Permissions.Application;
using SCDC.Modules.Community.Features.Permissions.Domain;
using SCDC.Modules.Community.Infrastructure.Persistence;

namespace SCDC.Modules.Community.Features.Permissions.Infrastructure;

internal sealed record ChannelLease(ManagementLease Server, ChannelState Channel, PermissionEvaluation Evaluation);
internal sealed class CommunityChannelGuard(CommunityManagementGuard management, ChannelReader reader) : IChannelAccessGuard
{
    public async Task<ChannelLease> AcquireCoreAsync(RelationalWorkScope scope, AccountActor actor, Guid server, Guid channel,
        bool write, string? permission, CancellationToken ct)
    {
        var lease = await management.AcquireAsync(scope, actor, server, write, false, ct, requiredPermission: null);
        var state = await reader.StateAsync(scope, server, channel, write, ct) ?? throw PermissionFailure.Missing();
        var evaluation = await reader.EvaluateAsync(scope, actor.UserId, server, state, lease, ct);
        if (!evaluation.CanView) throw PermissionFailure.Missing();
        if (permission is not null && !evaluation.ManagementPermissions.Contains(permission))
            throw new PermissionFailure(Error.Forbidden("PERMISSION_DENIED", "The required channel management permission is missing."));
        return new(lease, state, evaluation);
    }
    public async Task<ChannelAccessLease> AcquireAsync(AccountActor actor, Guid serverId, Guid channelId,
        DbTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction is not NpgsqlTransaction pg || pg.Connection is null) return new(ChannelAccessStatus.Unavailable);
        // This wrapper is not disposed: the caller owns this transaction and its locks.
        var scope = new RelationalWorkScope(pg.Connection, pg);
        try
        {
            var lease = await AcquireCoreAsync(scope, actor, serverId, channelId, false, null, cancellationToken);
            management.EnsureLease(lease.Server.Account);
            return new(ChannelAccessStatus.Allowed, lease.Server.MembershipId,
                lease.Server.AccessVersion.ToString(CultureInfo.InvariantCulture), lease.Channel.View.AccessVersion,
                lease.Evaluation.CanSendText, lease.Server.Account.SessionExpiresAt);
        }
        catch (PermissionFailure failure)
        {
            return new(failure.Error.Code switch
            {
                "SESSION_INVALID" => ChannelAccessStatus.InvalidSession,
                "ACCOUNT_ACCESS_DENIED" => ChannelAccessStatus.AccountDenied,
                "RESOURCE_NOT_FOUND" => ChannelAccessStatus.Hidden,
                _ => ChannelAccessStatus.Unavailable
            });
        }
        catch (NpgsqlException) { return new(ChannelAccessStatus.Unavailable); }
    }
}
