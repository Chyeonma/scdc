using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;
using SCDC.Media.Application;
using SCDC.Media.Domain;

namespace SCDC.Media.Infrastructure;

public sealed class CallStore(NpgsqlDataSource data, TimeProvider clock) : ICallStore
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private static NpgsqlCommand Command(NpgsqlConnection db, NpgsqlTransaction? tx, string sql, params (string, object?)[] args)
    {
        var command = new NpgsqlCommand(sql, db, tx) { CommandTimeout = 5 };
        foreach (var (key, value) in args) command.Parameters.AddWithValue(key, value ?? DBNull.Value);
        return command;
    }
    private static async Task<CallRecord?> Read(NpgsqlCommand command, CancellationToken ct)
    {
        await using (command) return await command.ExecuteScalarAsync(ct) is string json ? JsonSerializer.Deserialize<CallRecord>(json, Json)! : null;
    }
    private static async Task Write(NpgsqlCommand command, CancellationToken ct) { await using (command) await command.ExecuteNonQueryAsync(ct); }
    private static Task<CallRecord?> Read(NpgsqlConnection db, NpgsqlTransaction? tx, Guid id, CancellationToken ct) =>
        Read(Command(db, tx, "SELECT row_to_json(c)::text FROM media.calls c WHERE id=@id", ("id", id)), ct);
    private static async Task Lock(NpgsqlConnection db, NpgsqlTransaction tx, CancellationToken ct) =>
        await Write(Command(db, tx, "SET LOCAL lock_timeout='2s'; SELECT pg_advisory_xact_lock(1935893325,1)"), ct);
    public async Task<CallRecord> GetAsync(Guid id, CancellationToken ct)
    {
        await using var db = await data.OpenConnectionAsync(ct);
        return await Read(db, null, id, ct) ?? throw new MediaProblem(404, "Media.NotFound", "Call unavailable.");
    }
    public async Task<CallRecord?> CurrentAsync(Guid user, CancellationToken ct)
    {
        await using var db = await data.OpenConnectionAsync(ct);
        return await Read(Command(db, null, """
            SELECT row_to_json(c)::text FROM media.calls c WHERE (caller_id=@user OR peer_id=@user)
             AND(status<>'ended' OR updated_at>clock_timestamp()-interval '1 minute')
            ORDER BY (status='ended'),created_at DESC LIMIT 1
            """, ("user", user)), ct);
    }
    private static string Fingerprint(object body) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body))));
    private static async Task<CallRecord?> Replay(NpgsqlConnection db, NpgsqlTransaction tx, MediaActor actor, Guid op, string route, string fingerprint, CancellationToken ct)
    {
        await using var command = Command(db, tx, "SELECT route,fingerprint,call_id FROM media.operations WHERE actor_id=@actor AND operation_id=@op", ("actor", actor.UserId), ("op", op));
        Guid? id = null;
        await using (var rows = await command.ExecuteReaderAsync(ct))
            if (await rows.ReadAsync(ct))
            {
                if (rows.GetString(0) != route || rows.GetString(1) != fingerprint)
                    throw new MediaProblem(409, "Media.OperationConflict", "Operation ID was used for a different request.");
                id = rows.GetGuid(2);
            }
        return id is Guid value ? await Read(db, tx, value, ct) : null;
    }
    private static Task Receipt(NpgsqlConnection db, NpgsqlTransaction tx, MediaActor actor, Guid op, string route, string hash, Guid id, CancellationToken ct) =>
        Write(Command(db, tx, "INSERT INTO media.operations VALUES(@actor,@op,@route,@hash,@id)", ("actor", actor.UserId), ("op", op), ("route", route), ("hash", hash), ("id", id)), ct);
    public async Task<CallRecord> CreateAsync(MediaActor caller, MediaActor peer, Guid operation, CancellationToken ct)
    {
        CallRules.Uuid4(operation);
        if (caller.UserId == peer.UserId) throw new MediaProblem(400, "Media.InvalidPeer", "Choose another person.");
        var hash = Fingerprint(new { peer.UserId, caller.SessionId, caller.InstanceId });
        await using var db = await data.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await Lock(db, tx, ct);
        if (await Replay(db, tx, caller, operation, "create", hash, ct) is { } replay) return replay;
        await using (var busy = Command(db, tx, "SELECT EXISTS(SELECT 1 FROM media.user_claims WHERE user_id=ANY(@users))", ("users", new[] { caller.UserId, peer.UserId })))
            if (await busy.ExecuteScalarAsync(ct) is true) throw new MediaProblem(409, "Media.PeerBusy", "One participant is already in a call.");
        var id = Guid.CreateVersion7(); var now = clock.GetUtcNow();
        await Write(Command(db, tx, """
            INSERT INTO media.calls(id,caller_id,peer_id,caller_session_id,caller_instance_id,caller_name,peer_name,status,room_name,created_at,ring_expires_at)
            VALUES(@id,@caller,@peer,@session,@instance,@callerName,@peerName,'ringing',@room,@now,@expires);
            INSERT INTO media.user_claims(user_id,call_id) VALUES(@caller,@id),(@peer,@id)
            """, ("id", id), ("caller", caller.UserId), ("peer", peer.UserId), ("session", caller.SessionId), ("instance", caller.InstanceId),
            ("callerName", caller.User.DisplayName), ("peerName", peer.User.DisplayName), ("room", "call-" + id.ToString("N")), ("now", now), ("expires", now.AddSeconds(30))), ct);
        await Receipt(db, tx, caller, operation, "create", hash, id, ct);
        var result = (await Read(db, tx, id, ct))!; await tx.CommitAsync(ct); return result;
    }
    public async Task<CallRecord> ActAsync(Guid id, MediaActor actor, string action, string version, Guid operation, CancellationToken ct)
    {
        CallRules.Uuid4(operation);
        var route = id + "/" + action; var hash = Fingerprint(new { version, actor.SessionId, actor.InstanceId });
        await using var db = await data.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct); await Lock(db, tx, ct);
        if (await Replay(db, tx, actor, operation, route, hash, ct) is { } replay) return replay;
        var call = await Read(db, tx, id, ct) ?? throw new MediaProblem(404, "Media.NotFound", "Call unavailable.");
        CallRules.Action(call, actor, action, version, clock.GetUtcNow());
        if (action == "accept")
        {
            await using var capacity = Command(db, tx, "SELECT count(*) FROM media.user_claims u JOIN media.calls c ON c.id=u.call_id WHERE c.status='active' OR c.status='ended' AND NOT c.provider_closed");
            if ((long)(await capacity.ExecuteScalarAsync(ct))! > 18) throw new MediaProblem(409, "Media.SystemFull", "Media capacity is full.");
            await Write(Command(db, tx, "UPDATE media.calls SET status='active',peer_session_id=@session,peer_instance_id=@instance,version=version+1,updated_at=clock_timestamp() WHERE id=@id",
                ("session", actor.SessionId), ("instance", actor.InstanceId), ("id", id)), ct);
        }
        else await Finish(db, tx, call, action == "reject" ? "rejected" : action == "cancel" ? "cancelled" : "ended", ct);
        await Receipt(db, tx, actor, operation, route, hash, id, ct);
        var result = (await Read(db, tx, id, ct))!; await tx.CommitAsync(ct); return result;
    }
    private static async Task Finish(NpgsqlConnection db, NpgsqlTransaction tx, CallRecord call, string reason, CancellationToken ct)
    {
        if (call.Status == "ended") return;
        await Write(Command(db, tx, "UPDATE media.calls SET status='ended',end_reason=@reason,version=version+1,updated_at=clock_timestamp(),provider_closed=@closed WHERE id=@id",
            ("reason", reason), ("closed", call.Status == "ringing"), ("id", call.Id)), ct);
        if (call.Status == "ringing") await Write(Command(db, tx, "DELETE FROM media.user_claims WHERE call_id=@id", ("id", call.Id)), ct);
    }
    public async Task<CallRecord> EndAsync(Guid id, string reason, CancellationToken ct)
    {
        await using var db = await data.OpenConnectionAsync(ct); await using var tx = await db.BeginTransactionAsync(ct); await Lock(db, tx, ct);
        var call = await Read(db, tx, id, ct) ?? throw new MediaProblem(404, "Media.NotFound", "Call unavailable.");
        await Finish(db, tx, call, reason, ct); var result = (await Read(db, tx, id, ct))!; await tx.CommitAsync(ct); return result;
    }
    public async Task ClosedAsync(Guid id, CancellationToken ct)
    {
        await using var db = await data.OpenConnectionAsync(ct); await using var tx = await db.BeginTransactionAsync(ct); await Lock(db, tx, ct);
        await Write(Command(db, tx, "UPDATE media.calls SET provider_closed=true WHERE id=@id AND status='ended'; DELETE FROM media.user_claims WHERE call_id=@id AND EXISTS(SELECT 1 FROM media.calls WHERE id=@id AND status='ended' AND provider_closed)", ("id", id)), ct);
        await tx.CommitAsync(ct);
    }
    public async Task<IReadOnlyList<CallRecord>> PendingAsync(CancellationToken ct)
    {
        await using var db = await data.OpenConnectionAsync(ct); await using var command = Command(db, null, "SELECT row_to_json(c)::text FROM media.calls c WHERE status<>'ended' OR NOT provider_closed");
        await using var rows = await command.ExecuteReaderAsync(ct); var result = new List<CallRecord>();
        while (await rows.ReadAsync(ct)) result.Add(JsonSerializer.Deserialize<CallRecord>(rows.GetString(0), Json)!); return result;
    }
    public async Task RecoverAsync(CancellationToken ct)
    {
        foreach (var call in await PendingAsync(ct)) await EndAsync(call.Id, "service_restarted", ct);
    }
}
