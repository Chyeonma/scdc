using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Application.Text;
using SCDC.BuildingBlocks.Infrastructure.Outbox;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Features.Permissions.Domain;
using SCDC.Modules.Community.Features.Permissions.Infrastructure;
using SCDC.Modules.Community.Infrastructure;
using SCDC.Modules.Community.Infrastructure.Idempotency;
using SCDC.Modules.Community.Infrastructure.Paging;
using SCDC.Modules.Community.Infrastructure.Persistence;

namespace SCDC.Modules.Community.Features.Permissions.Application;

internal sealed class RoleService(RelationalWorkScopeFactory scopes, CommunityManagementGuard guard, RoleReader reader,
    ConfigurationCursorCodec cursors, IUserDirectory users, IOptionsMonitor<CommunityOptions> options,
    TransactionalOutbox outbox, TimeProvider clock, ILogger<RoleService> logger) : IRoleService
{
    public Task<Result<RolePage>> ListAsync(AccountActor actor,Guid server,int limit,string? cursor,CancellationToken ct)=>ExecuteAsync(async()=>
    {
        var last=Position("Roles",actor,server,limit,cursor);
        await using var scope=await scopes.OpenAsync(ct);
        var lease=await guard.AcquireAsync(scope,actor,server,false,false,ct);
        var items=await reader.ListAsync(scope,server,limit,last,ct);
        string? next=null;
        if(items.Count>limit){items.RemoveAt(items.Count-1);next=cursors.Encode("Roles",actor.UserId,server,limit,items[^1].Id);}
        guard.EnsureLease(lease.Account);await scope.CommitAsync(ct);
        return new RolePage(items,next);
    });
    public Task<Result<MemberPage>> MembersAsync(AccountActor actor,Guid server,int limit,string? cursor,CancellationToken ct)=>ExecuteAsync(async()=>
    {
        var last=Position("Members",actor,server,limit,cursor);
        await using var scope=await scopes.OpenAsync(ct);
        var lease=await guard.AcquireAsync(scope,actor,server,false,false,ct);
        var items=await reader.MembersAsync(scope,server,limit,last,ct);
        string? next=null;
        if(items.Count>limit){items.RemoveAt(items.Count-1);next=cursors.Encode("Members",actor.UserId,server,limit,items[^1].UserId);}
        var summaries=await users.FindByIdsAsync(items.Select(item=>item.UserId).ToArray(),ct);
        guard.EnsureLease(lease.Account);await scope.CommitAsync(ct);
        return new MemberPage(items.Select(item=>new MemberView(item,summaries.GetValueOrDefault(item.UserId))).ToArray(),next);
    });
    public Task<Result<MemberRoles>> MemberRolesAsync(AccountActor actor,Guid server,Guid user,CancellationToken ct)=>ExecuteAsync(async()=>
    {
        await using var scope=await scopes.OpenAsync(ct);
        var lease=await guard.AcquireAsync(scope,actor,server,false,true,ct);
        var result=await reader.MemberRolesAsync(scope,server,user,ct)??throw PermissionFailure.Missing();
        guard.EnsureLease(lease.Account);await scope.CommitAsync(ct);
        return result;
    });
    public Task<Result<CreateRoleResult>> CreateAsync(AccountActor actor,Guid server,CreateRoleCommand command,CancellationToken ct)=>ExecuteAsync(async()=>
    {
        if(command.ClientOperationId.Version!=4 || (command.ClientOperationId.ToByteArray(true)[8]&0xc0)!=0x80)
            throw PermissionFailure.Invalid("clientOperationId","A UUIDv4 with RFC variant is required.");
        var name=Name(command.Name);var permissions=Permissions(command.Permissions??[]);
        await using var scope=await scopes.OpenAsync(ct);
        var lease=await guard.AcquireAsync(scope,actor,server,true,true,ct);
        await using(var lookup=scope.CreateCommand("SELECT resource_id,fingerprint,key_id,fingerprint_version FROM community.operations WHERE actor_user_id=@actor AND kind='create_role' AND scope_id=@server AND client_operation_id=@op"))
        {
            lookup.Parameters.AddWithValue("actor",actor.UserId);lookup.Parameters.AddWithValue("server",server);lookup.Parameters.AddWithValue("op",command.ClientOperationId);
            Guid? resource=null;byte[]? fingerprint=null;string? keyId=null;short fingerprintVersion=1;
            await using(var rows=await lookup.ExecuteReaderAsync(ct))
                if(await rows.ReadAsync(ct)){resource=rows.GetGuid(0);fingerprint=rows.GetFieldValue<byte[]>(1);keyId=rows.GetString(2);fingerprintVersion=rows.GetInt16(3);}
            if(resource is Guid existing)
            {
                var hash=OperationFingerprint.ComputeRole(options.CurrentValue.Operations.GetKey(keyId!),actor.UserId,server,command.ClientOperationId,name,permissions,fingerprintVersion);
                if(!CryptographicOperations.FixedTimeEquals(hash,fingerprint!))throw PermissionFailure.Conflict("OPERATION_CONFLICT","Operation key was used with different data.");
                var current=await reader.GetAsync(scope,server,existing,ct)??throw PermissionFailure.Conflict("OPERATION_RESOURCE_REMOVED","The role created by this operation was removed.");
                guard.EnsureLease(lease.Account);await scope.CommitAsync(ct);
                return new CreateRoleResult(current,false);
            }
        }
        if((long)(await Scalar(scope,"SELECT count(*) FROM community.roles WHERE server_id=@server AND NOT is_system",ct,("server",server)))!>=20)
            throw PermissionFailure.Conflict("ROLE_LIMIT_REACHED","A community may have at most 20 custom roles.");
        Capacity(lease);
        var id=Guid.CreateVersion7();var keys=options.CurrentValue.Operations;var activeKey=keys.ActiveKeyId;
        var fingerprintNew=OperationFingerprint.ComputeRole(keys.GetKey(activeKey),actor.UserId,server,command.ClientOperationId,name,permissions);
        await Write(scope,"INSERT INTO community.roles(id,server_id,name,name_key,created_at,updated_at) VALUES(@id,@server,@name,@key,@now,@now)",ct,
            ("id",id),("server",server),("name",name),("key",UnicodeTextPolicy.NormalizeNameKey(name)),("now",clock.GetUtcNow()));
        await SetPermissions(scope,id,permissions,ct);
        await Write(scope,"INSERT INTO community.operations(actor_user_id,kind,scope_id,client_operation_id,fingerprint_version,key_id,fingerprint,resource_id) VALUES(@actor,'create_role',@server,@op,1,@key,@hash,@id)",ct,
            ("actor",actor.UserId),("server",server),("op",command.ClientOperationId),("key",activeKey),("hash",fingerprintNew),("id",id));
        await Changed(scope,server,lease,"role_created",id,null,null,ct);
        var role=await reader.GetAsync(scope,server,id,ct)??throw new InvalidOperationException("Created role missing.");
        guard.EnsureLease(lease.Account);await scope.CommitAsync(ct);
        return new CreateRoleResult(role,true);
    });
    public Task<Result<RoleView>> UpdateAsync(AccountActor actor,Guid server,Guid role,UpdateRoleCommand command,CancellationToken ct)=>ExecuteAsync(async()=>
    {
        var expected=Version(command.ExpectedVersion);
        if(!command.HasName&&!command.HasPermissions)throw PermissionFailure.Invalid("name","Specify a name or permissions to update.");
        var name=command.HasName?Name(command.Name):null;
        var permissions=command.HasPermissions?Permissions(command.Permissions):null;
        await using var scope=await scopes.OpenAsync(ct);
        var lease=await guard.AcquireAsync(scope,actor,server,true,true,ct);
        var current=await CustomRole(scope,server,role,expected,ct);
        name??=current.Name;permissions??=current.Permissions;
        if(name==current.Name&&permissions.ToHashSet(StringComparer.Ordinal).SetEquals(current.Permissions))
        {guard.EnsureLease(lease.Account);await scope.CommitAsync(ct);return current;}
        Capacity(lease,expected);
        await Write(scope,"UPDATE community.roles SET name=@name,name_key=@key WHERE id=@role AND server_id=@server",ct,
            ("name",name),("key",UnicodeTextPolicy.NormalizeNameKey(name)),("role",role),("server",server));
        await SetPermissions(scope,role,permissions,ct);
        await Changed(scope,server,lease,"role_updated",role,null,null,ct);
        var result=await reader.GetAsync(scope,server,role,ct)??throw new InvalidOperationException("Updated role missing.");
        guard.EnsureLease(lease.Account);await scope.CommitAsync(ct);
        return result;
    });
    public async Task<Result> DeleteAsync(AccountActor actor,Guid server,Guid role,string? expectedVersion,CancellationToken ct)
    {
        var response=await ExecuteAsync(async()=>
        {
            var expected=Version(expectedVersion);
            await using var scope=await scopes.OpenAsync(ct);
            var lease=await guard.AcquireAsync(scope,actor,server,true,true,ct);
            await CustomRole(scope,server,role,expected,ct);
            Capacity(lease,expected);
            if(await Scalar(scope,"SELECT EXISTS(SELECT 1 FROM community.server_members m JOIN community.member_roles mr ON mr.server_id=m.server_id AND mr.user_id=m.user_id AND mr.membership_id=m.membership_id WHERE mr.role_id=@role AND m.version=2147483647)",ct,("role",role)) is true)
                throw PermissionFailure.Conflict("VERSION_LIMIT_REACHED","A membership version limit has been reached.");
            if(await Scalar(scope,"SELECT EXISTS(SELECT 1 FROM community.invites WHERE server_id=@server AND default_role_id=@role)",ct,("server",server),("role",role)) is true)
                throw PermissionFailure.Conflict("ROLE_IN_USE","A legacy invitation still references this role.");
            await Write(scope,"UPDATE community.server_members m SET version=version+1 WHERE m.server_id=@server AND EXISTS(SELECT 1 FROM community.member_roles mr WHERE mr.server_id=m.server_id AND mr.user_id=m.user_id AND mr.membership_id=m.membership_id AND mr.role_id=@role)",ct,("server",server),("role",role));
            await Write(scope,"DELETE FROM community.roles WHERE id=@role AND server_id=@server",ct,("role",role),("server",server));
            await Changed(scope,server,lease,"role_deleted",role,null,null,ct);
            guard.EnsureLease(lease.Account);await scope.CommitAsync(ct);
            return true;
        });
        return response.IsSuccess?Result.Success():Result.Failure(response.Error);
    }
    public Task<Result<MemberRoles>> ReplaceAsync(AccountActor actor,Guid server,Guid user,ReplaceMemberRolesCommand command,CancellationToken ct)=>ExecuteAsync(async()=>
    {
        var expected=Version(command.ExpectedVersion);var roles=command.RoleIds;
        if(command.MembershipId==Guid.Empty)throw PermissionFailure.Invalid("membershipId","An active membership ID is required.");
        if(roles is null || roles.Count>20 || roles.Any(id=>id==Guid.Empty) || roles.Distinct().Count()!=roles.Count)
            throw PermissionFailure.Invalid("roleIds","Specify at most 20 distinct custom role IDs.");
        await using var scope=await scopes.OpenAsync(ct);
        var lease=await guard.AcquireAsync(scope,actor,server,true,true,ct);
        var current=await reader.MemberRolesAsync(scope,server,user,ct)??throw PermissionFailure.Missing();
        if(current.MembershipId!=command.MembershipId)throw PermissionFailure.Conflict("MEMBERSHIP_CHANGED","The member has a different membership epoch.");
        if(current.Version!=expected.ToString(CultureInfo.InvariantCulture))throw PermissionFailure.Conflict("VERSION_CONFLICT","The membership has changed. Reload before saving.");
        await using(var query=scope.CreateCommand("SELECT id,is_system FROM community.roles WHERE server_id=@server AND id=ANY(@roles)"))
        {
            query.Parameters.AddWithValue("server",server);query.Parameters.AddWithValue("roles",roles.ToArray());
            await using var rows=await query.ExecuteReaderAsync(ct);var count=0;
            while(await rows.ReadAsync(ct))
            {count++;if(rows.GetBoolean(1))throw PermissionFailure.Conflict("SYSTEM_ROLE_IMMUTABLE","@everyone cannot be assigned manually.");}
            if(count!=roles.Count)throw PermissionFailure.Invalid("roleIds","Every role must belong to this community.");
        }
        if(roles.ToHashSet().SetEquals(current.RoleIds))
        {guard.EnsureLease(lease.Account);await scope.CommitAsync(ct);return current;}
        Capacity(lease,expected);
        await Write(scope,"DELETE FROM community.member_roles WHERE server_id=@server AND user_id=@user; INSERT INTO community.member_roles(server_id,user_id,role_id,membership_id,assigned_by_user_id) SELECT @server,@user,id,@epoch,@actor FROM unnest(@roles::uuid[]) ids(id); UPDATE community.server_members SET version=version+1 WHERE server_id=@server AND user_id=@user AND membership_id=@epoch",ct,
            ("server",server),("user",user),("epoch",current.MembershipId),("actor",actor.UserId),("roles",roles.ToArray()));
        await Changed(scope,server,lease,"member_roles_changed",null,user,current.MembershipId,ct);
        var result=await reader.MemberRolesAsync(scope,server,user,ct)??throw new InvalidOperationException("Updated membership missing.");
        guard.EnsureLease(lease.Account);await scope.CommitAsync(ct);
        return result;
    });
    private async Task<RoleView> CustomRole(RelationalWorkScope scope,Guid server,Guid role,int expected,CancellationToken ct)
    {
        var current=await reader.GetAsync(scope,server,role,ct)??throw PermissionFailure.Missing();
        if(current.IsSystem)throw PermissionFailure.Conflict("SYSTEM_ROLE_IMMUTABLE","@everyone cannot be changed.");
        if(current.Version!=expected.ToString(CultureInfo.InvariantCulture))throw PermissionFailure.Conflict("VERSION_CONFLICT","The role has changed. Reload before saving.");
        return current;
    }
    private async Task Changed(RelationalWorkScope scope,Guid server,ManagementLease lease,string cause,Guid? role,Guid? user,Guid? membership,CancellationToken ct)
    {
        await Write(scope,"UPDATE community.servers SET access_version=access_version+1 WHERE id=@server",ct,("server",server));
        await outbox.AppendAsync(scope,"Community.AccessChanged.v1","community.server",server,lease.ServerVersion+1,
            new {serverId=server,accessVersion=(lease.AccessVersion+1).ToString(CultureInfo.InvariantCulture),cause,roleId=role,userId=user,membershipId=membership},ct);
    }
    private static void Capacity(ManagementLease lease,int version=1)
    {
        if(lease.ServerVersion==int.MaxValue||lease.AccessVersion==int.MaxValue||version==int.MaxValue)
            throw PermissionFailure.Conflict("VERSION_LIMIT_REACHED","The resource version limit has been reached.");
    }
    private Guid? Position(string collection,AccountActor actor,Guid server,int limit,string? cursor)
    {
        if(limit is <1 or >50)throw PermissionFailure.Invalid("limit","Limit must be between 1 and 50.");
        return cursor is null?null:cursors.Decode(collection,cursor,actor.UserId,server,limit);
    }
    private static int Version(string? value)
    {
        if(!int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out var version)||version<1||value!=version.ToString(CultureInfo.InvariantCulture))
            throw PermissionFailure.Invalid("expectedVersion","A positive resource version string is required.");
        return version;
    }
    private static string Name(string? name)
    {
        if(name is null||!UnicodeTextPolicy.IsValidUnicode(name))throw PermissionFailure.Invalid("name","A valid Unicode name is required.");
        name=UnicodeTextPolicy.TrimWhitespace(name);
        if(!UnicodeTextPolicy.IsValidName(name,1,64))throw PermissionFailure.Invalid("name","Name must contain 1–64 UTF-16 units of single-line text.");
        return name;
    }
    private static IReadOnlyList<string> Permissions(IReadOnlyList<string>? permissions)
    {
        if(permissions is null||permissions.Count>5||permissions.Any(code=>!ManagementPermissions.Contains(code))||permissions.Distinct(StringComparer.Ordinal).Count()!=permissions.Count)
            throw PermissionFailure.Invalid("permissions","Specify distinct supported management permissions.");
        return permissions.Order(StringComparer.Ordinal).ToArray();
    }
    private static async Task SetPermissions(RelationalWorkScope scope,Guid role,IReadOnlyList<string> permissions,CancellationToken ct)
        =>await Write(scope,"DELETE FROM community.role_permissions WHERE role_id=@role; INSERT INTO community.role_permissions(role_id,permission_code) SELECT @role,code FROM unnest(@permissions::text[]) codes(code)",ct,("role",role),("permissions",permissions.ToArray()));
    private static async Task Write(RelationalWorkScope scope,string sql,CancellationToken ct,params (string,object)[] parameters)
    {await using var query=scope.CreateCommand(sql);foreach(var(key,value)in parameters)query.Parameters.AddWithValue(key,value);await query.ExecuteNonQueryAsync(ct);}
    private static async Task<object?> Scalar(RelationalWorkScope scope,string sql,CancellationToken ct,params (string,object)[] parameters)
    {await using var query=scope.CreateCommand(sql);foreach(var(key,value)in parameters)query.Parameters.AddWithValue(key,value);return await query.ExecuteScalarAsync(ct);}
    private async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try{return Result.Success(await action());}
        catch(PermissionFailure ex){return Result.Failure<T>(ex.Error);}
        catch(FingerprintKeyUnavailableException){return Result.Failure<T>(Error.ServiceUnavailable("FINGERPRINT_KEY_UNAVAILABLE","The operation key is unavailable."));}
        catch(InvalidCursorException){return Result.Failure<T>(Error.Validation("CURSOR_INVALID","Cursor is invalid or expired."));}
        catch(CursorKeyUnavailableException){return Result.Failure<T>(Error.ServiceUnavailable("CURSOR_KEY_UNAVAILABLE","Cursor keys are unavailable."));}
        catch(PostgresException ex)when(ex is {SqlState:"23505",ConstraintName:"ux_role_name_key"})
        {return Result.Failure<T>(Error.Conflict("NAME_CONFLICT","A role with this name already exists."));}
        catch(Exception ex)when(Temporary(ex))
        {logger.LogWarning("Community role operation was interrupted ({FailureType}).",ex.GetType().Name);return Result.Failure<T>(Error.ServiceUnavailable("COMMUNITY_TEMPORARILY_UNAVAILABLE","Community is temporarily unavailable. Reload current state before retrying."));}
    }
    private static bool Temporary(Exception error)
    {
        for(Exception? cause=error;cause is not null;cause=cause.InnerException)
            if(cause is NpgsqlException{IsTransient:true}||cause is PostgresException{SqlState:"55P03" or "40P01" or "40001" or "57014"})return true;
        return false;
    }
}
