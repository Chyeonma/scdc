using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Community.Infrastructure.Authentication;

public sealed class PostgresCommunityFenceOwner(IConfiguration configuration) : ICommunityFenceOwner
{
    public AccessOwner Owner => AccessOwner.Community;
    public async Task<CommunityFenceReceipt> ApplyAsync(CommunityFenceBatch batch,CancellationToken ct)
    {
        if(batch.OperationId==Guid.Empty || batch.BatchId==Guid.Empty || batch.RoutingGeneration!=1
            || batch.Scopes.Any(s=>!Valid(s))
            || batch.Scopes.Select(Key).Distinct().Count()!=batch.Scopes.Count) throw new AccessCheckException("INVALID_FENCE");
        var scopes=batch.Scopes.OrderBy(Key,StringComparer.Ordinal).ToArray();
        var payload=JsonSerializer.Serialize(scopes);
        var fingerprint=Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"2:{batch.OperationId:D}:{batch.BatchId:D}:{payload}")));
        const string schema = "community";
        await using var connection=new NpgsqlConnection(configuration.GetConnectionString("Community")); await connection.OpenAsync(ct);
        await using var tx=await connection.BeginTransactionAsync(ct);
        await using(var budget=new NpgsqlCommand("SET LOCAL lock_timeout='500ms'; SET LOCAL statement_timeout='2s'; SELECT pg_advisory_xact_lock(1935893326,hashtext(@batch))",connection,tx))
        {budget.Parameters.AddWithValue("batch",batch.BatchId.ToString("D"));await budget.ExecuteNonQueryAsync(ct);}
        await using(var read=new NpgsqlCommand($"SELECT command_fingerprint FROM {schema}.fence_receipts WHERE authority=2 AND operation_id=@operation AND batch_id=@batch",connection,tx))
        {
            read.Parameters.AddWithValue("operation",batch.OperationId);read.Parameters.AddWithValue("batch",batch.BatchId);
            if(await read.ExecuteScalarAsync(ct) is string previous)
            {if(previous!=fingerprint)throw new AccessCheckException("FENCE_CONFLICT");await tx.CommitAsync(ct);return new(Owner,batch);}
        }
        foreach(var s in scopes)
        {
            await using var apply=new NpgsqlCommand($"""
                INSERT INTO {schema}.access_fences(authority,scope_kind,user_id,server_id,channel_id,membership_id,permission_code,min_epoch)
                VALUES(2,@kind,@user,@server,@channel,@member,@permission,@epoch)
                ON CONFLICT(authority,scope_kind,user_id,session_id,server_id,channel_id,membership_id,permission_code)
                DO UPDATE SET min_epoch=GREATEST({schema}.access_fences.min_epoch,excluded.min_epoch)
                """,connection,tx);
            apply.Parameters.AddWithValue("kind",s.Kind);apply.Parameters.AddWithValue("user",s.UserId);apply.Parameters.AddWithValue("server",s.ServerId);
            apply.Parameters.AddWithValue("channel",s.ChannelId);apply.Parameters.AddWithValue("member",s.MembershipId);
            apply.Parameters.AddWithValue("permission",s.Permission);apply.Parameters.AddWithValue("epoch",s.Epoch);await apply.ExecuteNonQueryAsync(ct);
        }
        await using(var receipt=new NpgsqlCommand($"INSERT INTO {schema}.fence_receipts(authority,operation_id,batch_id,command_fingerprint,routing_generation,installed_scopes) VALUES(2,@operation,@batch,@fingerprint,1,@scopes::jsonb)",connection,tx))
        {
            receipt.Parameters.AddWithValue("operation",batch.OperationId);receipt.Parameters.AddWithValue("batch",batch.BatchId);
            receipt.Parameters.AddWithValue("fingerprint",fingerprint);receipt.Parameters.AddWithValue("scopes",payload);await receipt.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);return new(Owner,batch);
    }
    private static string Key(CommunityScope s)=>$"{s.Kind}:{s.UserId:D}:{s.ServerId:D}:{s.ChannelId:D}:{s.MembershipId:D}:{s.Permission}";
    private static bool Valid(CommunityScope s)=>s.ServerId!=Guid.Empty && s.Epoch>0 && s.Kind switch
    {
        3=>s.UserId!=Guid.Empty&&s.MembershipId!=Guid.Empty&&s.ChannelId==Guid.Empty&&s.Permission=="",
        4=>s.UserId!=Guid.Empty&&s.MembershipId!=Guid.Empty&&s.ChannelId!=Guid.Empty&&s.Permission=="",
        5=>s.UserId!=Guid.Empty&&s.MembershipId!=Guid.Empty&&s.ChannelId==Guid.Empty&&new[]{"server_owner","manage_channels","manage_invites","review_join_requests","manage_join_mode","manage_channel_access"}.Contains(s.Permission),
        6=>s.UserId==Guid.Empty&&s.MembershipId==Guid.Empty&&s.ChannelId!=Guid.Empty&&s.Permission=="",
        _=>false
    };
}
