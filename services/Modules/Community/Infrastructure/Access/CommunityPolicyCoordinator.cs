using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Features.Channels.Application;
using SCDC.Modules.Community.Features.Permissions.Application;

namespace SCDC.Modules.Community.Infrastructure.Access;

internal sealed class CommunityPolicyCoordinator(RelationalWorkScopeFactory scopes,IServiceScopeFactory services,
    IEnumerable<ICommunityFenceOwner> owners) : ICommunityChangeStatusReader
{
    private readonly ICommunityFenceOwner[] fenceOwners=owners.OrderBy(o=>o.Owner).ToArray();
    public async Task<T> ExecuteAsync<T>(AccountActor actor,Guid server,CommunityPolicyCommand command,CancellationToken ct)
    {
        if(fenceOwners.Length!=2||!fenceOwners.Select(o=>o.Owner).SequenceEqual(new[]{AccessOwner.Community,AccessOwner.Messaging}))throw new AccessCheckException();
        await using var serviceScope=services.CreateAsyncScope();
        var access=serviceScope.ServiceProvider.GetRequiredService<CommunityIdentityAccess>();
        var permit=await access.AcquireAsync(actor,IdentityPurpose.CommunityManage,server,Guid.NewGuid(),ct);
        var fingerprint=Fingerprint(actor.UserId,actor.ExpectedMembershipId,server,command);
        Guid operation;
        await using(var local=await scopes.OpenAsync(ct))
        {
            var lease=await access.AdmitAsync(local,permit,ct);
            await LockServerAsync(local,server,ct);
            await using(var member=local.CreateCommand("SELECT membership_id FROM community.server_members WHERE server_id=@server AND user_id=@actor AND status=1"))
            {
                member.Parameters.AddWithValue("server",server);member.Parameters.AddWithValue("actor",actor.UserId);
                if(await member.ExecuteScalarAsync(ct) is not Guid current)throw PermissionFailure.Missing();
                if(current!=actor.ExpectedMembershipId)throw PermissionFailure.Conflict("MEMBERSHIP_CHANGED","Your membership changed. Reload before saving.");
            }
            await using(var pending=local.CreateCommand("SELECT operation_id,actor_user_id,command_fingerprint FROM community.access_changes WHERE server_id=@server AND phase=0"))
            {
                pending.Parameters.AddWithValue("server",server);
                await using var rows=await pending.ExecuteReaderAsync(ct);
                if(await rows.ReadAsync(ct))
                {
                    if(rows.GetGuid(1)!=actor.UserId||rows.GetString(2)!=fingerprint)throw new AccessCheckException("ACCESS_PENDING");
                    operation=rows.GetGuid(0);
                }
                else operation=Guid.Empty;
            }
            if(operation==Guid.Empty)
            {
                var before=await CommunityPolicySnapshot.ReadAsync(local,server,ct);
                await local.Transaction.SaveAsync("policy_preview",ct);
                var result=await ApplyAsync(serviceScope.ServiceProvider,local,actor,server,command,permit,null,ct);
                var after=await CommunityPolicySnapshot.ReadAsync(local,server,ct);
                var revoked=before.Allowed.Where(p=>!after.Allowed.ContainsKey(p.Key)).Select(p=>p.Value).OrderBy(CommunityScopeStore.Key,StringComparer.Ordinal).ToArray();
                if(revoked.Length==0)
                {
                    await access.EnsureAsync(local,lease,ct);await local.CommitAsync(ct);
                    return Convert<T>(result);
                }
                await local.Transaction.RollbackAsync("policy_preview",ct);
                operation=Guid.CreateVersion7();var next=new List<CommunityScope>();
                foreach(var stamp in revoked)
                {
                    await CommunityScopeStore.EnsureAsync(local,stamp,ct);
                    await using var prepare=local.CreateCommand("UPDATE community.access_scopes SET pending_operation_id=@operation,version=version+1 WHERE "+CommunityScopeStore.Match+" AND pending_operation_id IS NULL RETURNING current_epoch+1");
                    CommunityScopeStore.Bind(prepare,stamp);prepare.Parameters.AddWithValue("operation",operation);
                    if(await prepare.ExecuteScalarAsync(ct) is not long epoch)throw new AccessCheckException("ACCESS_PENDING");
                    next.Add(stamp with {Epoch=epoch});
                }
                await using var accept=local.CreateCommand("""
                    INSERT INTO community.access_changes(operation_id,server_id,actor_user_id,actor_session_id,actor_membership_id,
                        routing_generation,owners,command,command_fingerprint,scopes)
                    VALUES(@operation,@server,@actor,@session,@member,1,ARRAY['Community','Messaging'],@command::jsonb,@fingerprint,@scopes::jsonb)
                    """);
                accept.Parameters.AddWithValue("operation",operation);accept.Parameters.AddWithValue("server",server);
                accept.Parameters.AddWithValue("actor",actor.UserId);accept.Parameters.AddWithValue("session",actor.SessionId);
                accept.Parameters.AddWithValue("member",actor.ExpectedMembershipId??throw new AccessCheckException("MEMBERSHIP_REQUIRED"));
                accept.Parameters.AddWithValue("command",JsonSerializer.Serialize(command));accept.Parameters.AddWithValue("fingerprint",fingerprint);
                accept.Parameters.AddWithValue("scopes",JsonSerializer.Serialize(next));await accept.ExecuteNonQueryAsync(ct);
                await access.EnsureAsync(local,lease,ct);await local.CommitAsync(ct);
            }
        }
        // All authority and owner RPC work happens after releasing the accepting transaction.
        var wait=Stopwatch.StartNew();
        while(wait.Elapsed<TimeSpan.FromSeconds(3))
        {
            using var budget=CancellationTokenSource.CreateLinkedTokenSource(ct);budget.CancelAfter(TimeSpan.FromSeconds(3)-wait.Elapsed);
            try{await ProcessAsync(operation,budget.Token);}
            catch(Exception error)when(error is NpgsqlException or AccessCheckException or TimeoutException){}
            catch(OperationCanceledException)when(!ct.IsCancellationRequested){break;}
            (short Phase,string? Result,string? Error) completion;
            try { completion=await CompletionAsync(operation,actor.UserId,ct); }
            catch(NpgsqlException) { break; } // The accepted reference remains valid while the authority is unavailable.
            if(completion.Phase==1)return JsonSerializer.Deserialize<T>(completion.Result!)!;
            if(completion.Phase==2)throw new PermissionFailure(Error.Conflict(completion.Error??"ACCESS_CHANGED","The pending change was aborted. Reload before saving."));
            await Task.Delay(25,ct);
        }
        throw new CommunityChangePendingException(operation,server);
    }
    private static T Convert<T>(object? value)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    private static string Fingerprint(Guid actor,Guid? membership,Guid server,CommunityPolicyCommand command)
        =>System.Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"Community.Policy.v1:{actor:D}:{membership:D}:{server:D}:"+JsonSerializer.Serialize(command))));
    private static async Task LockServerAsync(RelationalWorkScope local,Guid server,CancellationToken ct)
    {
        await using var read=local.CreateCommand("SELECT id FROM community.servers WHERE id=@server FOR UPDATE");
        read.Parameters.AddWithValue("server",server);if(await read.ExecuteScalarAsync(ct) is null)throw PermissionFailure.Missing();
    }
    private static Task<object?> ApplyAsync(IServiceProvider provider,RelationalWorkScope local,AccountActor actor,Guid server,
        CommunityPolicyCommand command,IdentityPermit permit,Guid? operation,CancellationToken ct)
    {
        if(command.Kind=="channel_access")return ((ChannelService)provider.GetRequiredService<IChannelService>()).ReplacePolicyAsync(local,actor,server,command.ResourceId,command.Read<ReplaceChannelAccessCommand>(),permit,operation,ct);
        var roles=(RoleService)provider.GetRequiredService<IRoleService>();
        return command.Kind switch
        {
            "role_update"=>roles.UpdatePolicyAsync(local,actor,server,command.ResourceId,command.Read<UpdateRoleCommand>(),permit,operation,ct),
            "role_delete"=>roles.DeletePolicyAsync(local,actor,server,command.ResourceId,command.Read<string>(),permit,operation,ct),
            "member_roles"=>roles.ReplacePolicyAsync(local,actor,server,command.ResourceId,command.Read<ReplaceMemberRolesCommand>(),permit,operation,ct),
            _=>throw new AccessCheckException("INVALID_POLICY_COMMAND")
        };
    }
    public async Task<bool> ProcessAsync(Guid operation,CancellationToken ct)
    {
        var leaseId=Guid.NewGuid();CommunityPolicyWork work;
        await using(var claimScope=await scopes.OpenAsync(ct))
        {
            await using var claim=claimScope.CreateCommand("""
                UPDATE community.access_changes SET lease_token=@lease,lease_until=clock_timestamp()+interval '10 seconds',version=version+1
                WHERE operation_id=@operation AND phase=0 AND next_attempt_at<=clock_timestamp()
                    AND (lease_until IS NULL OR lease_until<clock_timestamp())
                RETURNING server_id,actor_user_id,actor_session_id,actor_membership_id,command::text,scopes::text
                """);
            claim.Parameters.AddWithValue("lease",leaseId);claim.Parameters.AddWithValue("operation",operation);
            await using(var row=await claim.ExecuteReaderAsync(ct))
            {
                if(!await row.ReadAsync(ct))return false;
                work=new(operation,row.GetGuid(0),row.GetGuid(1),row.GetGuid(2),row.GetGuid(3),
                    JsonSerializer.Deserialize<CommunityPolicyCommand>(row.GetString(4))!,JsonSerializer.Deserialize<CommunityScope[]>(row.GetString(5))!);
            }
            await claimScope.CommitAsync(ct);
        }
        try
        {
            foreach(var owner in fenceOwners)
            {
                var stamps=work.Scopes.Where(s=>owner.Owner==AccessOwner.Community||s.Kind!=5).ToArray();
                var batch=new CommunityFenceBatch(operation,operation,1,Array.AsReadOnly(stamps));
                using var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct);deadline.CancelAfter(TimeSpan.FromSeconds(2));
                var receipt=await owner.ApplyAsync(batch,deadline.Token).WaitAsync(deadline.Token);
                if(receipt.Owner!=owner.Owner||JsonSerializer.Serialize(receipt.Batch)!=JsonSerializer.Serialize(batch))throw new AccessCheckException();
                await using var ackScope=await scopes.OpenAsync(ct);
                await using(var ack=ackScope.CreateCommand("""
                    INSERT INTO community.access_change_acks(operation_id,batch_id,owner,routing_generation,installed_scopes)
                    VALUES(@operation,@operation,@owner,1,@scopes::jsonb) ON CONFLICT DO NOTHING
                    """))
                {
                    ack.Parameters.AddWithValue("operation",operation);ack.Parameters.AddWithValue("owner",owner.Owner.ToString());
                    ack.Parameters.AddWithValue("scopes",JsonSerializer.Serialize(stamps));await ack.ExecuteNonQueryAsync(ct);
                }
                await ackScope.CommitAsync(ct);
            }
            await using var serviceScope=services.CreateAsyncScope();var access=serviceScope.ServiceProvider.GetRequiredService<CommunityIdentityAccess>();
            var actor=new AccountActor(work.ActorId,work.SessionId,Guid.Empty,DateTimeOffset.MaxValue,work.MembershipId);
            IdentityPermit? permit=null;string? failure=null;
            try{permit=await access.AcquireAsync(actor,IdentityPurpose.CommunityManage,work.ServerId,operation,ct,resuming:true);}
            catch(AccessCheckException error)when(error.Code is "Common.Unauthorized" or "ACCOUNT_ACCESS_DENIED"){failure="ACCESS_CHANGED";}
            await using var local=await scopes.OpenAsync(ct);
            CommunityAccountLease? account=null;
            if(permit is not null)account=await access.AdmitAsync(local,permit,ct);
            await LockServerAsync(local,work.ServerId,ct);
            await using(var check=local.CreateCommand("""
                SELECT phase=0 AND lease_token=@lease AND lease_until>clock_timestamp()
                    AND (SELECT count(*) FROM community.access_change_acks a WHERE a.operation_id=c.operation_id)=2
                FROM community.access_changes c WHERE operation_id=@operation FOR UPDATE
                """))
            {
                check.Parameters.AddWithValue("lease",leaseId);check.Parameters.AddWithValue("operation",operation);
                if(await check.ExecuteScalarAsync(ct) is not true)return false;
            }
            object? result=null;
            if(failure is null)
            {
                await local.Transaction.SaveAsync("policy_apply",ct);
                try
                {
                    result=await ApplyAsync(serviceScope.ServiceProvider,local,actor,work.ServerId,work.Command,permit!,operation,ct);
                    await access.EnsureAsync(local,account!,ct);
                }
                catch(PermissionFailure error){await local.Transaction.RollbackAsync("policy_apply",ct);failure=error.Error.Code;}
                catch(AccessCheckException error)when(error.Code=="ACCESS_CHANGED"){await local.Transaction.RollbackAsync("policy_apply",ct);failure=error.Code;}
            }
            // The installed floors are never rolled back, including when the original actor lost access.
            foreach(var stamp in work.Scopes.OrderBy(CommunityScopeStore.Key,StringComparer.Ordinal))
            {
                await using var advance=local.CreateCommand("UPDATE community.access_scopes SET current_epoch=@epoch,pending_operation_id=NULL,version=version+1 WHERE "+CommunityScopeStore.Match+" AND pending_operation_id=@operation");
                CommunityScopeStore.Bind(advance,stamp);advance.Parameters.AddWithValue("epoch",stamp.Epoch);advance.Parameters.AddWithValue("operation",operation);
                if(await advance.ExecuteNonQueryAsync(ct)!=1)throw new AccessCheckException();
            }
            await using(var finish=local.CreateCommand("""
                UPDATE community.access_changes SET phase=@phase,result=@result::jsonb,error_code=@error,completed_at=clock_timestamp(),
                    version=version+1,lease_token=NULL,lease_until=NULL WHERE operation_id=@operation AND lease_token=@lease
                """))
            {
                finish.Parameters.AddWithValue("phase",failure is null?(short)1:(short)2);finish.Parameters.AddWithValue("result",JsonSerializer.Serialize(result));
                finish.Parameters.AddWithValue("error",NpgsqlTypes.NpgsqlDbType.Text,(object?)failure??DBNull.Value);
                finish.Parameters.AddWithValue("operation",operation);finish.Parameters.AddWithValue("lease",leaseId);await finish.ExecuteNonQueryAsync(ct);
            }
            await local.CommitAsync(ct);return true;
        }
        catch
        {
            using var retryBudget=new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await using var retryScope=await scopes.OpenAsync(retryBudget.Token);
            await using(var retry=retryScope.CreateCommand("""
                UPDATE community.access_changes SET lease_token=NULL,lease_until=NULL,attempt_count=attempt_count+1,version=version+1,
                    next_attempt_at=clock_timestamp()+make_interval(secs=>LEAST(30,0.25*power(2,LEAST(attempt_count,7)))*(0.8+random()*0.4))
                WHERE operation_id=@operation AND phase=0 AND lease_token=@lease
                """))
            {retry.Parameters.AddWithValue("operation",operation);retry.Parameters.AddWithValue("lease",leaseId);await retry.ExecuteNonQueryAsync(retryBudget.Token);}
            await retryScope.CommitAsync(retryBudget.Token);throw;
        }
    }
    private async Task<(short Phase,string? Result,string? Error)> CompletionAsync(Guid operation,Guid actor,CancellationToken ct)
    {
        await using var local=await scopes.OpenAsync(ct);
        await using var query=local.CreateCommand("SELECT phase,result::text,error_code FROM community.access_changes WHERE operation_id=@operation AND actor_user_id=@actor");
        query.Parameters.AddWithValue("operation",operation);query.Parameters.AddWithValue("actor",actor);await using var rows=await query.ExecuteReaderAsync(ct);
        if(!await rows.ReadAsync(ct))throw new AccessCheckException();
        return(rows.GetInt16(0),rows.IsDBNull(1)?null:rows.GetString(1),rows.IsDBNull(2)?null:rows.GetString(2));
    }
    public async Task<CommunityChangeStatus?> ReadAsync(Guid operationId,AccountActor actor,Guid serverId,CancellationToken ct)
    {
        try
        {
            await using var serviceScope=services.CreateAsyncScope();var access=serviceScope.ServiceProvider.GetRequiredService<CommunityIdentityAccess>();
            var permit=await access.AcquireAsync(actor,IdentityPurpose.AccountRead,serverId,operationId,ct);
            await using var local=await scopes.OpenAsync(ct);var lease=await access.AdmitAsync(local,permit,ct);
            CommunityChangeStatus? status=null;
            await using(var query=local.CreateCommand("""
                SELECT c.phase,c.version,c.error_code FROM community.access_changes c
                JOIN community.servers s ON s.id=c.server_id
                JOIN community.server_members m ON m.server_id=s.id AND m.user_id=@actor AND m.status=1
                WHERE c.operation_id=@operation AND c.server_id=@server AND s.status=1 AND s.deleted_at IS NULL
                    AND (c.actor_user_id=@actor OR s.owner_user_id=@actor) FOR SHARE OF s,m
                """))
            {
                query.Parameters.AddWithValue("operation",operationId);query.Parameters.AddWithValue("actor",actor.UserId);query.Parameters.AddWithValue("server",serverId);
                await using var rows=await query.ExecuteReaderAsync(ct);
                if(await rows.ReadAsync(ct))status=new(operationId,rows.GetInt16(0) switch{0=>"fencing",1=>"committed",_=>"aborted"},rows.GetInt64(1).ToString(CultureInfo.InvariantCulture),false,rows.IsDBNull(2)?null:rows.GetString(2));
            }
            await access.EnsureAsync(local,lease,ct);await local.CommitAsync(ct);return status;
        }
        catch(NpgsqlException) { throw new AccessCheckException(); }
    }
}
