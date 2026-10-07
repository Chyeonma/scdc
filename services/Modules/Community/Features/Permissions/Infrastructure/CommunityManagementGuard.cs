using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Features.Permissions.Application;
using SCDC.Modules.Community.Features.Permissions.Domain;
using Npgsql;

namespace SCDC.Modules.Community.Features.Permissions.Infrastructure;

internal sealed record ManagementLease(AccountAccessCheck Account, int ServerVersion, int AccessVersion);
internal sealed class CommunityManagementGuard(IAccountAccessGuard accounts, TimeProvider clock)
{
    public async Task<ManagementLease> AcquireAsync(RelationalWorkScope scope, AccountActor actor, Guid server, bool write, bool ownerOnly, CancellationToken ct)
    {
        AccountAccessCheck account;
        try { account=await accounts.AcquireAsync(actor,scope.Transaction,ct); }
        catch(NpgsqlException){throw new PermissionFailure(Error.ServiceUnavailable("ACCESS_CHECK_UNAVAILABLE","Account access could not be checked."));}
        if(account.Status==AccountAccessStatus.AccountDenied)throw new PermissionFailure(Error.Forbidden("ACCOUNT_ACCESS_DENIED","An active account with verified primary email is required."));
        EnsureLease(account);
        Guid owner;int version,access;
        await using(var query=scope.CreateCommand($"SELECT owner_user_id,version,access_version,status,deleted_at IS NOT NULL FROM community.servers WHERE id=@id FOR {(write ? "UPDATE" : "SHARE")}"))
        {
            query.Parameters.AddWithValue("id",server);
            await using var rows=await query.ExecuteReaderAsync(ct);
            if(!await rows.ReadAsync(ct)||rows.GetInt16(3)!=1||rows.GetBoolean(4))throw PermissionFailure.Missing();
            owner=rows.GetGuid(0);version=rows.GetInt32(1);access=rows.GetInt32(2);
        }
        await using(var member=scope.CreateCommand("SELECT membership_id FROM community.server_members WHERE server_id=@server AND user_id=@actor AND status=1"))
        {
            member.Parameters.AddWithValue("server",server);member.Parameters.AddWithValue("actor",actor.UserId);
            if(await member.ExecuteScalarAsync(ct) is not Guid)throw PermissionFailure.Missing();
        }
        if(owner!=actor.UserId)
        {
            if(ownerOnly)throw new PermissionFailure(Error.Forbidden("PERMISSION_DENIED","Only the current owner can manage roles."));
            var grants=new List<RoleGrant>();
            await using var query=scope.CreateCommand("""
                SELECT rp.permission_code FROM community.member_roles mr
                JOIN community.server_members m ON m.server_id=mr.server_id AND m.user_id=mr.user_id AND m.membership_id=mr.membership_id
                JOIN community.roles r ON r.id=mr.role_id AND r.server_id=mr.server_id
                JOIN community.role_permissions rp ON rp.role_id=r.id
                WHERE mr.server_id=@server AND mr.user_id=@actor AND m.status=1 AND NOT r.is_system
                """);
            query.Parameters.AddWithValue("server",server);query.Parameters.AddWithValue("actor",actor.UserId);
            await using var rows=await query.ExecuteReaderAsync(ct);
            while(await rows.ReadAsync(ct))grants.Add(new(ViewEffect.Inherit,[rows.GetString(0)]));
            if(!PermissionEvaluator.Management(true,false,grants).Contains("manage_channel_access"))
                throw new PermissionFailure(Error.Forbidden("PERMISSION_DENIED","Role catalog access requires manage_channel_access."));
        }
        return new(account,version,access);
    }
    public void EnsureLease(AccountAccessCheck account)
    {
        if(!account.IsAllowedAt(clock.GetUtcNow()))throw new PermissionFailure(Error.Unauthorized("SESSION_INVALID","The session is no longer valid."));
    }
}
