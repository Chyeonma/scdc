using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Community.Infrastructure.Authentication;

public sealed class PostgresIdentityFenceOwner(IConfiguration configuration) : IIdentityFenceOwner
{
    public AccessOwner Owner => AccessOwner.Community;
    public async Task<IdentityFenceReceipt> ApplyAsync(IdentityFenceBatch batch, CancellationToken ct)
    {
        if (batch.OperationId == Guid.Empty || batch.BatchId == Guid.Empty || batch.RoutingGeneration != 1
            || batch.Scopes.Count == 0 || batch.Scopes.Any(s => s.UserId == Guid.Empty || s.Epoch <= 0
                || s.Kind is not (1 or 2) || (s.Kind == 1) != (s.SessionId == Guid.Empty))
            || batch.Scopes.Select(s => (s.Kind, s.UserId, s.SessionId)).Distinct().Count() != batch.Scopes.Count)
            throw new AccessCheckException("INVALID_FENCE");
        var scopes = batch.Scopes.OrderBy(s => s.Kind).ThenBy(s => s.UserId.ToString("D"), StringComparer.Ordinal)
            .ThenBy(s => s.SessionId.ToString("D"), StringComparer.Ordinal).ToArray();
        var payload = JsonSerializer.Serialize(scopes);
        var fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"1:{batch.OperationId:D}:{batch.BatchId:D}:{payload}")));
        const string schema = "community";
        await using var connection = new NpgsqlConnection(configuration.GetConnectionString("Community"));
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var gate = new NpgsqlCommand("SET LOCAL lock_timeout='500ms'; SET LOCAL statement_timeout='2s'; SELECT pg_advisory_xact_lock(1935893325,hashtext(@batch))", connection, transaction))
        { gate.Parameters.AddWithValue("batch", batch.BatchId.ToString("D")); await gate.ExecuteNonQueryAsync(ct); }
        await using (var lookup = new NpgsqlCommand($"SELECT command_fingerprint FROM {schema}.fence_receipts WHERE authority=1 AND operation_id=@operation AND batch_id=@batch", connection, transaction))
        {
            lookup.Parameters.AddWithValue("operation", batch.OperationId); lookup.Parameters.AddWithValue("batch", batch.BatchId);
            if (await lookup.ExecuteScalarAsync(ct) is string existing)
            {
                if (existing != fingerprint) throw new AccessCheckException("FENCE_CONFLICT");
                await transaction.CommitAsync(ct);
                return new(Owner, batch);
            }
        }
        foreach (var scope in scopes)
        {
            await using var apply = new NpgsqlCommand($"""
                INSERT INTO {schema}.access_fences(authority,scope_kind,user_id,session_id,min_epoch)
                VALUES(1,@kind,@user,@session,@epoch)
                ON CONFLICT(authority,scope_kind,user_id,session_id,server_id,channel_id,membership_id,permission_code)
                DO UPDATE SET min_epoch=GREATEST({schema}.access_fences.min_epoch,excluded.min_epoch)
                """, connection, transaction);
            apply.Parameters.AddWithValue("kind", scope.Kind); apply.Parameters.AddWithValue("user", scope.UserId);
            apply.Parameters.AddWithValue("session", scope.SessionId); apply.Parameters.AddWithValue("epoch", scope.Epoch);
            await apply.ExecuteNonQueryAsync(ct);
        }
        await using (var receipt = new NpgsqlCommand($"""
            INSERT INTO {schema}.fence_receipts(authority,operation_id,batch_id,command_fingerprint,routing_generation,installed_scopes)
            VALUES(1,@operation,@batch,@fingerprint,@generation,@scopes::jsonb)
            """, connection, transaction))
        {
            receipt.Parameters.AddWithValue("operation", batch.OperationId); receipt.Parameters.AddWithValue("batch", batch.BatchId);
            receipt.Parameters.AddWithValue("fingerprint", fingerprint); receipt.Parameters.AddWithValue("generation", batch.RoutingGeneration);
            receipt.Parameters.AddWithValue("scopes", payload); await receipt.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return new(Owner, batch);
    }
}
