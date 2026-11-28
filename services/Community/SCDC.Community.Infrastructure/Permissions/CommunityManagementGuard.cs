using SCDC.BuildingBlocks.Application.Results;
using SCDC.Modules.Community.Infrastructure.Persistence;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Features.Permissions.Application;
using SCDC.Modules.Community.Features.Permissions.Domain;
using Npgsql;
using SCDC.Modules.Community.Infrastructure.Access;

namespace SCDC.Modules.Community.Features.Permissions.Infrastructure;

internal sealed record ManagementLease(CommunityAccountLease Account, int ServerVersion, int AccessVersion,
    Guid MembershipId, bool IsOwner, IReadOnlyList<string> Permissions);
internal sealed class CommunityManagementGuard(CommunityIdentityAccess accounts)
{
    public async Task<ManagementLease> AcquireAsync(RelationalWorkScope scope, AccountActor actor, Guid server, bool write, bool ownerOnly, CancellationToken ct,
        IdentityPermit permit, string? requiredPermission = "manage_channel_access", Guid? acceptedOperation = null)
    {
        var account=await accounts.AdmitAsync(scope,permit,ct);
        Guid owner;int version,access;
        await using(var query=scope.CreateCommand($"SELECT owner_user_id,version,access_version,status,deleted_at IS NOT NULL FROM community.servers WHERE id=@id FOR {(write ? "UPDATE" : "SHARE")}"))
        {
            query.Parameters.AddWithValue("id",server);
            await using var rows=await query.ExecuteReaderAsync(ct);
            if(!await rows.ReadAsync(ct)||rows.GetInt16(3)!=1||rows.GetBoolean(4))throw PermissionFailure.Missing();
            owner=rows.GetGuid(0);version=rows.GetInt32(1);access=rows.GetInt32(2);
        }
        Guid membershipId;
        await using(var member=scope.CreateCommand("SELECT membership_id FROM community.server_members WHERE server_id=@server AND user_id=@actor AND status=1"))
        {
            member.Parameters.AddWithValue("server",server);member.Parameters.AddWithValue("actor",actor.UserId);
            if(await member.ExecuteScalarAsync(ct) is not Guid epoch)throw PermissionFailure.Missing();
            membershipId = epoch;
        }
        if(write && actor.ExpectedMembershipId!=membershipId)
            throw PermissionFailure.Conflict("MEMBERSHIP_CHANGED","Your membership changed. Reload before saving.");
        if(write)
        {
            await using var pending=scope.CreateCommand("SELECT operation_id FROM community.access_changes WHERE server_id=@server AND phase=0");
            pending.Parameters.AddWithValue("server",server);
            if(await pending.ExecuteScalarAsync(ct) is Guid operation && operation!=acceptedOperation)
                throw new AccessCheckException("ACCESS_PENDING");
        }
        var required=new List<SCDC.Contracts.Community.CommunityScope>{CommunityScopeStore.Membership(actor.UserId,server,membershipId)};
        if(ownerOnly) required.Add(CommunityScopeStore.Management(actor.UserId,server,membershipId,"server_owner"));
        else if(requiredPermission is not null)required.Add(CommunityScopeStore.Management(actor.UserId,server,membershipId,requiredPermission));
        await CommunityScopeStore.CheckAsync(scope,required,acceptedOperation,ct);
        IReadOnlyList<string> permissions = ManagementPermissions.Codes.Order(StringComparer.Ordinal).ToArray();
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
            permissions = PermissionEvaluator.Management(true,false,grants);
            if(requiredPermission is not null && !permissions.Contains(requiredPermission))
                throw new PermissionFailure(Error.Forbidden("PERMISSION_DENIED","The required management permission is missing."));
        }
        return new(account,version,access,membershipId,owner==actor.UserId,permissions);
    }
    public Task<IdentityPermit> PrepareAsync(AccountActor actor, Guid server, bool write, CancellationToken ct, IdentityPurpose? purpose = null)
        =>accounts.AcquireAsync(actor,purpose??(write?IdentityPurpose.CommunityManage:IdentityPurpose.AccountRead),server,Guid.NewGuid(),ct);
    public Task EnsureAsync(RelationalWorkScope scope, CommunityAccountLease account, CancellationToken ct)
        =>accounts.EnsureAsync(scope,account,ct);
}
