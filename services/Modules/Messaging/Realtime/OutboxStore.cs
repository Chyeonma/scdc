using Npgsql;
using SCDC.Contracts.Persistence;

namespace SCDC.Modules.Messaging.Realtime;

internal sealed record OutboxClaim(Guid Id, Guid SpaceId, Guid MessageId, Guid LeaseToken, int Attempt, DateTimeOffset LeaseExpiresAt);
internal sealed class OutboxStore(ISharedDatabaseSession session)
{
    public async Task<OutboxClaim?> ClaimAsync(int leaseSeconds, Guid? spaceId, CancellationToken token)
    {
        await using var owner = await session.BeginAsync(token);
        var lease = Guid.NewGuid();
        await using var command = new NpgsqlCommand("""
            WITH candidate AS (
             SELECT id FROM integration.outbox_events
             WHERE event_type='Messaging.MessageChanged' AND aggregate_type='Message' AND space_id IS NOT NULL
              AND published_at IS NULL AND available_at<=clock_timestamp()
              AND (lease_expires_at IS NULL OR lease_expires_at<=clock_timestamp())
              AND (@space IS NULL OR space_id=@space)
             ORDER BY occurred_at,id FOR UPDATE SKIP LOCKED LIMIT 1
            ) UPDATE integration.outbox_events o
            SET lease_token=@lease,lease_expires_at=clock_timestamp()+make_interval(secs=>@seconds),
                attempt_count=attempt_count+1,last_error=NULL
            FROM candidate c WHERE o.id=c.id
            RETURNING o.id,o.space_id,o.aggregate_id,o.attempt_count,o.lease_expires_at
            """, (NpgsqlConnection)session.Connection, (NpgsqlTransaction)session.Transaction!);
        command.Parameters.AddWithValue("lease", lease); command.Parameters.AddWithValue("seconds", leaseSeconds);
        command.Parameters.AddWithValue("space", NpgsqlTypes.NpgsqlDbType.Uuid, (object?)spaceId ?? DBNull.Value);
        OutboxClaim? claim = null;
        await using (var reader = await command.ExecuteReaderAsync(token))
            if (await reader.ReadAsync(token)) claim = new(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), lease,
                reader.GetInt32(3), reader.GetFieldValue<DateTimeOffset>(4));
        await owner.CommitAsync(token); return claim;
    }
    public async Task CompleteAsync(OutboxClaim claim, string? error, CancellationToken token)
    {
        await using var owner = await session.BeginAsync(token);
        await using var command = new NpgsqlCommand("""
            UPDATE integration.outbox_events SET
             published_at=CASE WHEN @success THEN clock_timestamp() ELSE published_at END,
             available_at=CASE WHEN @success THEN available_at ELSE clock_timestamp()+make_interval(secs=>@backoff) END,
             last_error=@error,lease_token=NULL,lease_expires_at=NULL
            WHERE id=@id AND lease_token=@lease
            """, (NpgsqlConnection)session.Connection, (NpgsqlTransaction)session.Transaction!);
        command.Parameters.AddWithValue("success", error is null);
        command.Parameters.AddWithValue("backoff", 1 << Math.Min(claim.Attempt - 1, 5));
        command.Parameters.AddWithValue("error", NpgsqlTypes.NpgsqlDbType.Text, (object?)error ?? DBNull.Value);
        command.Parameters.AddWithValue("id", claim.Id); command.Parameters.AddWithValue("lease", claim.LeaseToken);
        await command.ExecuteNonQueryAsync(token); await owner.CommitAsync(token);
    }
}
