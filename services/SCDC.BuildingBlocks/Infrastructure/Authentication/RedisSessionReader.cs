using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SCDC.Contracts.Identity;
using StackExchange.Redis;

namespace SCDC.BuildingBlocks.Infrastructure.Authentication;

public sealed class RedisSessionConnection : IDisposable
{
    private readonly Lazy<ConnectionMultiplexer> connection;
    public string Prefix { get; }
    public RedisSessionConnection(IConfiguration configuration, string role)
    {
        Prefix = configuration["Authentication:Sessions:KeyPrefix"] ?? "scdc:auth:";
        if (Prefix.Length is < 1 or > 100 || Prefix.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ':' and not '-' and not '_'))
            throw new InvalidOperationException("Invalid authentication Redis prefix.");
        var endpoint = configuration["Authentication:Sessions:Endpoint"]
            ?? throw new InvalidOperationException("Authentication:Sessions:Endpoint is required.");
        var passwordFile = configuration[$"Authentication:Sessions:{role}PasswordFile"];
        var options = ConfigurationOptions.Parse(endpoint);
        options.AbortOnConnectFail = false;
        options.AllowAdmin = role == "Writer"; // INFO only; the server ACL remains the authority.
        options.ConfigurationChannel = "";
        options.CommandMap = CommandMap.Create(new HashSet<string>
            { "SUBSCRIBE", "PSUBSCRIBE", "UNSUBSCRIBE", "PUNSUBSCRIBE", "CONFIG", "CLUSTER", "SENTINEL", "GET", "SET" }, available: false);
        options.ConnectRetry = 0;
        options.ConnectTimeout = 500;
        options.AsyncTimeout = 500;
        options.SyncTimeout = 500;
        options.BacklogPolicy = BacklogPolicy.FailFast;
        options.User = configuration[$"Authentication:Sessions:{role}User"];
        if (!string.IsNullOrWhiteSpace(passwordFile)) options.Password = File.ReadAllText(passwordFile).Trim();
        connection = new(() => ConnectionMultiplexer.Connect(options));
    }
    public IDatabase Database => connection.Value.GetDatabase();
    public void Dispose() { if (connection.IsValueCreated) connection.Value.Dispose(); }
}

public sealed record RedisSession(Guid Id, DateTimeOffset ExpiresAt, bool Revoked, bool Pending, long Epoch);
public sealed record RedisAccount(Guid UserId, long Version, int Status, bool Verified, Guid? Stamp,
    bool Pending, long Epoch, RedisSession[] Sessions);

public sealed class RedisSessionReader(RedisSessionConnection connection, TimeProvider clock) : IAuthenticatedSessionReader, ISessionStoreReadiness
{
    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            return (long)await connection.Database.ScriptEvaluateAsync("""
                local run=string.match(redis.call('INFO','server'),'run_id:([^\r\n]+)')
                local meta=redis.call('HMGET',KEYS[1],'ready','run')
                if meta[1]=='1' and meta[2]==run then return 1 end
                return 0
                """, [connection.Prefix + "ready"]).WaitAsync(cancellationToken) == 1;
        }
        catch (Exception error) when (error is RedisException or RedisCommandException or TimeoutException) { return false; }
    }
    // Checking the server run_id inside the same script rejects a restored ready marker after restart.
    public const string ReadScript = """
        local info=redis.call('INFO','server')
        local run=string.match(info,'run_id:([^\r\n]+)')
        local meta=redis.call('HMGET',KEYS[1],'ready','generation','run')
        if meta[1]~='1' or meta[3]~=run then return false end
        local row=redis.call('HMGET',KEYS[2],'generation','payload')
        if row[1]~=meta[2] or not row[2] then return false end
        return row[2]
        """;
    public async Task<SessionAccess> ReadAsync(AuthenticatedSession actor, CancellationToken cancellationToken, bool requireVerifiedEmail = true)
    {
        if (actor.AccessExpiresAt <= clock.GetUtcNow()) return new(false, "session_expired");
        try
        {
            var result = await connection.Database.ScriptEvaluateAsync(ReadScript,
                [connection.Prefix + "ready", connection.Prefix + "user:" + actor.UserId.ToString("D")]).WaitAsync(cancellationToken);
            if (result.IsNull) throw new AccessCheckException();
            var row = JsonSerializer.Deserialize<RedisAccount>((string)result!, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new AccessCheckException();
            if (row.UserId != actor.UserId || row.Version <= 0 || row.Epoch <= 0 || row.Sessions is null
                || row.Sessions.Any(s => s.Id == Guid.Empty || s.Epoch <= 0))
                throw new AccessCheckException();
            if (row.Pending) throw new AccessCheckException("ACCESS_PENDING");
            if (row.Stamp != actor.SecurityStamp) return new(false, "session_revoked");
            if (row.Status != 1 || (requireVerifiedEmail && !row.Verified)) return new(false, "account_unavailable");
            var session = row.Sessions.SingleOrDefault(s => s.Id == actor.SessionId);
            if (session is null || session.Revoked) return new(false, "session_revoked");
            if (session.Pending) throw new AccessCheckException("ACCESS_PENDING");
            return session.ExpiresAt > clock.GetUtcNow() ? new(true) : new(false, "session_expired");
        }
        catch (Exception error) when (error is RedisException or RedisCommandException or TimeoutException or JsonException or InvalidOperationException)
        { throw new AccessCheckException(); }
    }
}
