using System.Globalization;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SCDC.BuildingBlocks.Infrastructure.Authentication;
using SCDC.Contracts.Identity;
using StackExchange.Redis;

namespace SCDC.Modules.Identity.Infrastructure.Services;

/// <summary>Only Identity resolves the writer connection. A journal survives every publication failure.</summary>
internal sealed class SessionPublisher(IConfiguration configuration) : IDisposable
{
    private readonly RedisSessionConnection writer = new(configuration, "Writer");
    private readonly SemaphoreSlim gate = new(1);
    private readonly ConcurrentDictionary<Guid, long> published = new();
    private string? currentGeneration;
    private readonly string database = configuration.GetConnectionString("Identity")!;
    // Compare decimal bigint strings, never Lua floating point numbers (epochs may exceed 2^53).
    internal const string PublishScript = """
        local generation=redis.call('HGET',KEYS[1],'generation')
        if generation~=ARGV[1] then return -1 end
        local old=redis.call('HGET',KEYS[2],'version')
        local oldgen=redis.call('HGET',KEYS[2],'generation')
        if oldgen==generation and old and (#old>#ARGV[2] or (#old==#ARGV[2] and old>ARGV[2])) then return 0 end
        redis.call('HSET',KEYS[2],'generation',generation,'version',ARGV[2],'payload',ARGV[3])
        return 1
        """;
    private const string SnapshotSql = """
        SELECT p.version, jsonb_build_object(
            'userId',p.user_id,'version',p.version,
            'status',CASE WHEN u.deleted_at IS NULL THEN COALESCE(u.status,0) ELSE 0 END,
            'verified',EXISTS(SELECT 1 FROM identity.user_emails e WHERE e.user_id=u.id AND e.is_primary AND e.verified_at IS NOT NULL),
            'stamp',security.security_stamp,'epoch',COALESCE(a.current_epoch,1),
            'pending',a.pending_operation_id IS NOT NULL,
            'sessions',COALESCE((SELECT jsonb_agg(jsonb_build_object(
                'id',s.id,'expiresAt',s.expires_at,'revoked',s.revoked_at IS NOT NULL,
                'pending',f.pending_operation_id IS NOT NULL,'epoch',COALESCE(f.current_epoch,1)))
                FROM identity.auth_sessions s LEFT JOIN identity.access_scopes f
                ON f.scope_kind=2 AND f.user_id=s.user_id AND f.session_id=s.id WHERE s.user_id=u.id),'[]'::jsonb))::text
        FROM identity.session_publications p
        LEFT JOIN identity.users u ON u.id=p.user_id
        LEFT JOIN identity.user_security_states security ON security.user_id=u.id
        LEFT JOIN identity.access_scopes a ON a.scope_kind=1 AND a.user_id=u.id
        WHERE p.user_id=@user
        """;

    public async Task PublishUserAsync(Guid user, CancellationToken ct)
    {
        try
        {
            await SynchronizeAsync(ct);
            // Generation is read BEFORE the DB snapshot; stale work cannot enter a rebuilt store.
            var generation = await writer.Database.HashGetAsync(writer.Prefix + "ready", "generation").WaitAsync(ct);
            await PublishAsync(user, generation.ToString(), ct);
        }
        catch (Exception error) when (error is RedisException or RedisCommandException or NpgsqlException or TimeoutException)
        { throw new AccessCheckException(); }
    }

    public async Task SynchronizeAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            await using var connection = new NpgsqlConnection(database);
            await connection.OpenAsync(ct);
            var redis = writer.Database;
            var info = (string)(await redis.ExecuteAsync("INFO", "server").WaitAsync(ct))!;
            var run = info.Split('\n').Single(line => line.StartsWith("run_id:", StringComparison.Ordinal))[7..].Trim();
            var readyKey = writer.Prefix + "ready";
            var meta = await redis.HashGetAsync(readyKey, ["generation", "run", "ready"]).WaitAsync(ct);
            var locked = false;
            try
            {
                if (meta[0].IsNull || meta[1] != run || meta[2] != "1")
                {
                    // Only rebuild needs the cross-process lock. Healthy publishers reconcile concurrently.
                    await using var locking = new NpgsqlCommand("SELECT pg_try_advisory_lock(1935893324,hashtext(@prefix))", connection);
                    locking.Parameters.AddWithValue("prefix", writer.Prefix);
                    var started = System.Diagnostics.Stopwatch.StartNew();
                    while (!(locked = await locking.ExecuteScalarAsync(ct) is true))
                    {
                        if (started.Elapsed > TimeSpan.FromSeconds(2)) throw new AccessCheckException();
                        await Task.Delay(20, ct);
                    }
                    meta = await redis.HashGetAsync(readyKey, ["generation", "run", "ready"]).WaitAsync(ct);
                }
                string generation;
                if (meta[0].IsNull || meta[1] != run || meta[2] != "1")
                {
                    await using var next = new NpgsqlCommand("SELECT nextval('identity.access_store_generation')", connection);
                    generation = Convert.ToInt64(await next.ExecuteScalarAsync(ct)).ToString(CultureInfo.InvariantCulture);
                    await redis.HashSetAsync(readyKey, [new("generation", generation), new("run", run), new("ready", "0")]).WaitAsync(ct);
                    published.Clear();
                }
                else generation = meta[0].ToString();
                if (currentGeneration != generation) { published.Clear(); currentGeneration = generation; }
                // Repeat to a stable journal watermark while admission is closed during a rebuild.
                for (var pass = 0; pass < 10; pass++)
                {
                    var changes = new List<(Guid User, long Version)>();
                    await using (var journal = new NpgsqlCommand("SELECT user_id,version FROM identity.session_publications", connection))
                    await using (var rows = await journal.ExecuteReaderAsync(ct))
                        while (await rows.ReadAsync(ct))
                            if (!published.TryGetValue(rows.GetGuid(0), out var version) || version < rows.GetInt64(1)) changes.Add((rows.GetGuid(0), rows.GetInt64(1)));
                    if (changes.Count == 0) break;
                    foreach (var chunk in changes.Chunk(128))
                    {
                        var observations = await Task.WhenAll(chunk.Select(async change =>
                        {
                            var stored = await redis.HashGetAsync(writer.Prefix + "user:" + change.User.ToString("D"), ["generation", "version"]).WaitAsync(ct);
                            return (change, stored);
                        }));
                        var missing = new List<Guid>();
                        foreach (var (change, stored) in observations)
                        {
                            if (stored[0] == generation && long.TryParse(stored[1].ToString(), out var existing) && existing >= change.Version)
                                published.AddOrUpdate(change.User, existing, (_, old) => Math.Max(old, existing));
                            else missing.Add(change.User);
                        }
                        if (missing.Count > 0) await PublishSnapshotsAsync(connection, missing.ToArray(), generation, ct);
                    }
                    if (pass == 9) throw new AccessCheckException();
                }
                await redis.ScriptEvaluateAsync("""
                    if redis.call('HGET',KEYS[1],'generation')~=ARGV[1] then return 0 end
                    redis.call('HSET',KEYS[1],'ready','1')
                    redis.call('PEXPIRE',KEYS[1],2000)
                    return 1
                    """, [readyKey], [generation]).WaitAsync(ct);
            }
            finally
            {
                if (locked)
                {
                    await using var unlocking = new NpgsqlCommand("SELECT pg_advisory_unlock(1935893324,hashtext(@prefix))", connection);
                    unlocking.Parameters.AddWithValue("prefix", writer.Prefix);
                    await unlocking.ExecuteScalarAsync(CancellationToken.None);
                }
            }
        }
        finally { gate.Release(); }
    }

    private async Task PublishAsync(Guid user, string generation, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(database);
        await connection.OpenAsync(ct);
        await PublishSnapshotsAsync(connection, [user], generation, ct);
    }

    private async Task PublishSnapshotsAsync(NpgsqlConnection connection, Guid[] users, string generation, CancellationToken ct)
    {
        // One DB snapshot and bounded Redis pipelines avoid a round trip per historical account
        // delaying readiness or consuming the 2-second permission lease during cold startup.
        var sql = SnapshotSql.Replace("SELECT p.version,", "SELECT p.user_id,p.version,", StringComparison.Ordinal)
            .Replace("WHERE p.user_id=@user", "WHERE p.user_id=ANY(@users)", StringComparison.Ordinal);
        var snapshots = new List<(Guid User, long Version, string Payload)>();
        await using (var snapshot = new NpgsqlCommand(sql, connection))
        {
            snapshot.Parameters.AddWithValue("users", users);
            await using var rows = await snapshot.ExecuteReaderAsync(ct);
            while (await rows.ReadAsync(ct)) snapshots.Add((rows.GetGuid(0), rows.GetInt64(1), rows.GetString(2)));
        }
        if (snapshots.Count != users.Length) throw new AccessCheckException();
        await Task.WhenAll(snapshots.Select(async row =>
        {
            var result = (long)await writer.Database.ScriptEvaluateAsync(PublishScript,
                [writer.Prefix + "ready", writer.Prefix + "user:" + row.User.ToString("D")],
                [generation, row.Version.ToString(CultureInfo.InvariantCulture), row.Payload]).WaitAsync(ct);
            if (result < 0) throw new AccessCheckException();
            published.AddOrUpdate(row.User, row.Version, (_, old) => Math.Max(old, row.Version));
        }));
    }
    public void Dispose() { writer.Dispose(); gate.Dispose(); }
}
