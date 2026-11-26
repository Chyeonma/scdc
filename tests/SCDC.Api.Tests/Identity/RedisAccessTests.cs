using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using SCDC.Api.Tests.Infrastructure;
using SCDC.BuildingBlocks.Infrastructure.Authentication;
using SCDC.Contracts.Identity;
using SCDC.DbMigrator;
using SCDC.Modules.Identity.Infrastructure.Services;
using StackExchange.Redis;

namespace SCDC.Api.Tests.Identity;

public sealed class RedisAccessDatabase : IAsyncLifetime
{
    public string Admin { get; private set; } = "";
    public Dictionary<string, string?> Configuration { get; } = [];
    private string root = "";
    private readonly string name = "s206_" + Guid.NewGuid().ToString("N") + "_test";
    public async Task InitializeAsync()
    {
        var settings = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        root = settings.GetConnectionString("TestDatabase")!;
        await using (var connection = new NpgsqlConnection(root))
        {
            await connection.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE {name}", connection); await create.ExecuteNonQueryAsync();
        }
        Admin = new NpgsqlConnectionStringBuilder(root) { Database = name, Pooling = false }.ConnectionString;
        var bootstrap = await File.ReadAllLinesAsync(Path.Combine(Repo(), "database/postgres/schema.sql"));
        await Sql(string.Join('\n', bootstrap.Where(line => !line.StartsWith('\\'))));
        var roles = await File.ReadAllTextAsync(Path.Combine(Repo(), "database/postgres/module-roles.sql"));
        await Sql("BEGIN;\n" + roles[roles.IndexOf("REVOKE CREATE ON SCHEMA public FROM PUBLIC;", StringComparison.Ordinal)..]);
        await IdentityEmailTestDatabase.UpgradeAsync(Admin);
        foreach (var owner in new[] { "Identity", "Community", "Messaging" })
        {
            var original = settings.GetConnectionString(owner) ?? root;
            Configuration["ConnectionStrings:" + owner] = new NpgsqlConnectionStringBuilder(original) { Database = name }.ConnectionString;
        }
        Configuration["Authentication:Sessions:KeyPrefix"] = "scdc:auth:" + name + ":";
    }
    public async Task<object?> Sql(string sql, Guid? id = null)
    {
        await using var connection = new NpgsqlConnection(Admin); await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        if (id is Guid value) command.Parameters.AddWithValue("id", value);
        return await command.ExecuteScalarAsync();
    }
    public static string Repo()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SCDC.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
    public async Task DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(root); await connection.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE {name} WITH (FORCE)", connection); await drop.ExecuteNonQueryAsync();
    }
}

public sealed class RedisAccessTests(RedisAccessDatabase database) : IClassFixture<RedisAccessDatabase>, IAsyncLifetime
{
    private readonly SCDCWebApplicationFactory original = new();
    private WebApplicationFactory<ApiProgram> app = null!;
    private HttpClient client = null!;
    private FaultingOwner owner = null!;
    private IConfiguration settings = null!;
    private readonly string username = "redis_" + Guid.NewGuid().ToString("N")[..14];
    private const string Password = "RedisTestPassword123";
    public async Task InitializeAsync()
    {
        app = original.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(database.Configuration));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IIdentityFenceOwner>();
                services.AddSingleton<IIdentityFenceOwner>(provider => new PostgresIdentityFenceOwner(provider.GetRequiredService<IConfiguration>(), AccessOwner.Community));
                services.AddSingleton<IIdentityFenceOwner>(provider => owner = new FaultingOwner(new PostgresIdentityFenceOwner(provider.GetRequiredService<IConfiguration>(), AccessOwner.Messaging)));
            });
        });
        client = app.CreateClient(); settings = app.Services.GetRequiredService<IConfiguration>();
        await SCDCWebApplicationFactory.WaitForAccessReadyAsync(client);
    }
    private async Task<JsonElement> Login(bool register = true)
    {
        if (register)
        {
            var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new { username, displayName = "Redis access", email = username + "@example.test", password = Password });
            registration.EnsureSuccessStatusCode(); var body = await registration.Content.ReadFromJsonAsync<JsonElement>();
            (await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = body.GetProperty("developmentVerificationToken").GetString() })).EnsureSuccessStatusCode();
        }
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { login = username, password = Password });
        response.EnsureSuccessStatusCode(); return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    private static AuthenticatedSession Actor(JsonElement auth)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(auth.GetProperty("accessToken").GetString());
        string Claim(string name) => token.Claims.Single(c => c.Type == name).Value;
        return new(Guid.Parse(Claim("sub")), Guid.Parse(Claim("sid")), Guid.Parse(Claim("sst")), new DateTimeOffset(token.ValidTo, TimeSpan.Zero));
    }
    private Task<HttpResponseMessage> Me(JsonElement auth) => Authorized(HttpMethod.Get, "/api/v1/users/me", auth);
    private Task<HttpResponseMessage> Authorized(HttpMethod method, string path, JsonElement auth)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());
        return client.SendAsync(request);
    }
    private async Task Wait(Func<Task<bool>> predicate)
    {
        var timer = Stopwatch.StartNew();
        while (!await predicate()) { Assert.True(timer.Elapsed < TimeSpan.FromSeconds(15), "Access reconciliation timed out."); await Task.Delay(50); }
    }

    [Fact]
    public async Task Login_publishes_before_returning_and_refresh_preserves_the_session_deadline()
    {
        await app.Services.GetRequiredService<SessionPublisher>().SynchronizeAsync(default);
        var auth = await Login(); Assert.Equal(HttpStatusCode.OK, (await Me(auth)).StatusCode);
        var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = auth.GetProperty("refreshToken").GetString() });
        refresh.EnsureSuccessStatusCode(); var next = await refresh.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(Actor(auth).SessionId, Actor(next).SessionId);
        Assert.Equal(auth.GetProperty("refreshTokenExpiresAt").GetString(), next.GetProperty("refreshTokenExpiresAt").GetString());
    }

    [Fact]
    public async Task Revocation_waits_for_both_owners_and_survives_a_lost_receipt()
    {
        var auth = await Login(); owner.LoseNextReceipt = true;
        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = auth.GetProperty("refreshToken").GetString() });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Me(auth)).StatusCode);
        var actor = Actor(auth);
        Assert.Equal(2L, await database.Sql("SELECT count(*) FROM identity.access_change_acks a JOIN identity.access_changes c USING(operation_id) WHERE c.user_id=@id", actor.UserId));
        Assert.True(owner.Calls >= 2);
        await Assert.ThrowsAsync<PostgresException>(() => database.Sql("UPDATE identity.access_changes SET payload=payload||'{\"changed\":true}'::jsonb,version=version+1 WHERE user_id=@id", actor.UserId));
        await Assert.ThrowsAsync<PostgresException>(() => database.Sql("UPDATE identity.access_changes SET phase=0,version=version+1 WHERE user_id=@id", actor.UserId));
    }

    [Fact]
    public async Task Revocation_accepts_a_receipt_reconstructed_across_a_transport_boundary()
    {
        var auth = await Login(); owner.ReconstructReceipt = true;
        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = auth.GetProperty("refreshToken").GetString() });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Me(auth)).StatusCode);
        Assert.Equal(2L, await database.Sql("SELECT count(*) FROM identity.access_change_acks a JOIN identity.access_changes c USING(operation_id) WHERE c.user_id=@id", Actor(auth).UserId));
    }

    [Fact]
    public async Task A_receipt_for_different_scopes_cannot_acknowledge_or_finalize_revocation()
    {
        var auth = await Login(); var other = await Login(false); owner.CorruptReceipt = true;
        var response = await Authorized(HttpMethod.Delete, "/api/v1/auth/sessions/" + Actor(auth).SessionId, other);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<JsonElement>(); var operation = status.GetProperty("operationId").GetGuid();
        Assert.Equal(0L, await database.Sql("SELECT count(*) FROM identity.access_change_acks WHERE operation_id=@id AND owner='Messaging'", operation));
        Assert.Equal(0L, await database.Sql("SELECT count(*) FROM identity.auth_sessions WHERE id=@id AND revoked_at IS NOT NULL", Actor(auth).SessionId));
        owner.CorruptReceipt = false;
        await Wait(async () => (await Me(auth)).StatusCode == HttpStatusCode.Unauthorized);
        Assert.Equal(2L, await database.Sql("SELECT count(*) FROM identity.access_change_acks WHERE operation_id=@id", operation));
    }

    [Fact]
    public async Task An_owner_ignoring_cancellation_cannot_stall_revocation_and_recovery_reuses_the_work()
    {
        var auth = await Login(); var other = await Login(false);
        owner.Stall = new(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var response = await Authorized(HttpMethod.Delete, "/api/v1/auth/sessions/" + Actor(auth).SessionId, other)
                .WaitAsync(TimeSpan.FromSeconds(9));
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            var status = await response.Content.ReadFromJsonAsync<JsonElement>(); var operation = status.GetProperty("operationId").GetGuid();
            Assert.Equal(0L, await database.Sql("SELECT count(*) FROM identity.auth_sessions WHERE id=@id AND revoked_at IS NOT NULL", Actor(auth).SessionId));
            Assert.Equal(0L, await database.Sql("SELECT count(*) FROM identity.access_change_acks WHERE operation_id=@id AND owner='Messaging'", operation));
            owner.Stall.TrySetResult(); owner.Stall = null;
            await Wait(async () => (await Me(auth)).StatusCode == HttpStatusCode.Unauthorized);
            Assert.Equal(2L, await database.Sql("SELECT count(*) FROM identity.access_change_acks WHERE operation_id=@id", operation));
            Assert.Equal(1L, await database.Sql("SELECT count(*) FROM identity.access_changes WHERE user_id=@id", Actor(auth).UserId));
        }
        finally { owner.Stall?.TrySetResult(); owner.Stall = null; }
    }

    [Fact]
    public async Task Unavailable_owner_keeps_revocation_pending_and_worker_finishes_after_recovery()
    {
        var auth = await Login(); var other = await Login(false); owner.Blocked = true;
        var response = await Authorized(HttpMethod.Delete, "/api/v1/auth/sessions/" + Actor(auth).SessionId, other);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<JsonElement>(); var operation = status.GetProperty("operationId").GetGuid();
        Assert.Equal(0L, await database.Sql("SELECT count(*) FROM identity.auth_sessions WHERE id=@id AND revoked_at IS NOT NULL", Actor(auth).SessionId));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await Me(auth)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Me(other)).StatusCode);
        owner.Blocked = false;
        await Wait(async () => (await Me(auth)).StatusCode == HttpStatusCode.Unauthorized);
        Assert.Equal("committed", (await app.Services.GetRequiredService<IRevocationStatusReader>().ReadAsync(operation, Actor(auth).UserId, default))!.Phase);
        Assert.Equal(HttpStatusCode.OK, (await Me(other)).StatusCode);
    }

    [Fact]
    public async Task Missing_record_is_unavailable_and_reading_does_not_fall_back_to_Identity()
    {
        var auth = await Login(); var actor = Actor(auth);
        var prefix = settings["Authentication:Sessions:KeyPrefix"]!;
        using var connection = new RedisSessionConnection(settings, "Writer");
        // Keep a valid ready marker, remove only this account, and read before the journal worker.
        await connection.Database.KeyDeleteAsync(prefix + "user:" + actor.UserId.ToString("D"));
        await Assert.ThrowsAsync<AccessCheckException>(() => app.Services.GetRequiredService<IAuthenticatedSessionReader>().ReadAsync(actor, default));
    }

    [Fact]
    public async Task Restored_ready_marker_from_an_old_Redis_process_is_rejected()
    {
        var auth = await Login(); var actor = Actor(auth);
        using var connection = new RedisSessionConnection(settings, "Writer");
        var key = settings["Authentication:Sessions:KeyPrefix"] + "ready";
        await connection.Database.HashSetAsync(key, "run", "old-process");
        await Assert.ThrowsAsync<AccessCheckException>(() => app.Services.GetRequiredService<IAuthenticatedSessionReader>().ReadAsync(actor, default));
        await app.Services.GetRequiredService<SessionPublisher>().SynchronizeAsync(default);
        Assert.True((await app.Services.GetRequiredService<IAuthenticatedSessionReader>().ReadAsync(actor, default)).Allowed);
    }

    [Fact]
    public async Task Publication_compares_bigint_versions_exactly_and_rejects_old_generations()
    {
        using var connection = new RedisSessionConnection(settings, "Writer"); var redis = connection.Database;
        var prefix = settings["Authentication:Sessions:KeyPrefix"] + "test:";
        await redis.HashSetAsync(prefix + "ready", "generation", "15");
        async Task<long> Publish(string generation, string version, string payload) => (long)await redis.ScriptEvaluateAsync(SessionPublisher.PublishScript,
            [prefix + "ready", prefix + "user"], [generation, version, payload]);
        Assert.Equal(1, await Publish("15", "9007199254740993", "deny"));
        Assert.Equal(0, await Publish("15", "9007199254740992", "active"));
        Assert.Equal(-1, await Publish("14", "9223372036854775807", "active"));
        Assert.Equal("deny", (string?)await redis.HashGetAsync(prefix + "user", "payload"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Corrupt_or_mismatched_account_snapshot_is_unavailable_instead_of_revoking_the_user(bool wrongAccount)
    {
        var auth = await Login(); var actor = Actor(auth);
        using var connection = new RedisSessionConnection(settings, "Writer");
        var payload = JsonSerializer.Serialize(new
        {
            userId = wrongAccount ? Guid.NewGuid() : actor.UserId,
            version = 1,
            status = 1,
            verified = true,
            stamp = actor.SecurityStamp,
            epoch = 1,
            sessions = (object?)null
        });
        await connection.Database.HashSetAsync(settings["Authentication:Sessions:KeyPrefix"] + "user:" + actor.UserId.ToString("D"), "payload", payload);
        await Assert.ThrowsAsync<AccessCheckException>(() => app.Services.GetRequiredService<IAuthenticatedSessionReader>().ReadAsync(actor, default));
        await app.Services.GetRequiredService<SessionPublisher>().PublishUserAsync(actor.UserId, default);
        Assert.Equal(HttpStatusCode.OK, (await Me(auth)).StatusCode);
    }

    [Fact]
    public async Task Peer_policy_and_owner_purpose_are_enforced_when_acquiring_permits()
    {
        var auth = await Login(); var actor = Actor(auth);
        await using var scope = app.Services.CreateAsyncScope();
        var issuer = scope.ServiceProvider.GetRequiredKeyedService<IIdentityPermitIssuer>(AccessOwner.Messaging);
        var permit = await issuer.AcquireAsync(new(actor, IdentityPurpose.DmRead, Guid.NewGuid(), Guid.NewGuid()), default);
        Assert.Equal(2, permit.Scopes.Count); Assert.True(permit.ExpiresAt - permit.IssuedAt <= TimeSpan.FromSeconds(5));
        var missingPeer = Guid.NewGuid();
        await Assert.ThrowsAsync<AccessCheckException>(() => issuer.AcquireAsync(new(actor, IdentityPurpose.DmSendNew, Guid.NewGuid(), Guid.NewGuid(), missingPeer), default));
        await Assert.ThrowsAsync<AccessCheckException>(() => issuer.AcquireAsync(new(actor, IdentityPurpose.CommunityManage, Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Theory]
    [InlineData(AccessOwner.Community)]
    [InlineData(AccessOwner.Messaging)]
    public async Task Fence_replay_and_out_of_order_batches_cannot_lower_or_replace_it(AccessOwner target)
    {
        var user = Guid.NewGuid(); var operation = Guid.NewGuid();
        var batch = new IdentityFenceBatch(operation, operation, 1, [new(1, user, Guid.Empty, 9)]);
        var fence = new PostgresIdentityFenceOwner(settings, target);
        await fence.ApplyAsync(batch, default); await fence.ApplyAsync(batch, default);
        await fence.ApplyAsync(batch with { BatchId = Guid.NewGuid(), Scopes = [new(1, user, Guid.Empty, 2)] }, default);
        await Assert.ThrowsAsync<AccessCheckException>(() => fence.ApplyAsync(batch with { Scopes = [new(1, user, Guid.Empty, 1)] }, default));
        Assert.Equal(9L, await database.Sql($"SELECT min_epoch FROM {target.ToString().ToLowerInvariant()}.access_fences WHERE user_id=@id", user));
    }

    [Theory]
    [InlineData(AccessOwner.Community)]
    [InlineData(AccessOwner.Messaging)]
    public async Task Fence_batch_advisory_lock_is_bounded_before_waiting(AccessOwner target)
    {
        var operation = Guid.NewGuid();
        var batch = new IdentityFenceBatch(operation, operation, 1, [new(1, Guid.NewGuid(), Guid.Empty, 2)]);
        await using var connection = new NpgsqlConnection(database.Admin); await connection.OpenAsync();
        await using var hold = new NpgsqlCommand("SELECT pg_advisory_lock(1935893325,hashtext(@batch))", connection);
        hold.Parameters.AddWithValue("batch", batch.BatchId.ToString("D")); await hold.ExecuteNonQueryAsync();
        var timer = Stopwatch.StartNew();
        var failure = await Assert.ThrowsAsync<PostgresException>(() =>
            new PostgresIdentityFenceOwner(settings, target).ApplyAsync(batch, default).WaitAsync(TimeSpan.FromSeconds(3)));
        Assert.Equal(PostgresErrorCodes.LockNotAvailable, failure.SqlState);
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(2));
        Assert.Equal(0L, await database.Sql($"SELECT count(*) FROM {target.ToString().ToLowerInvariant()}.fence_receipts WHERE batch_id=@id", operation));
    }

    [Fact]
    public async Task Revocation_status_is_private_to_its_account()
    {
        var first = await Login();
        await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = first.GetProperty("refreshToken").GetString() });
        var operation = (Guid)(await database.Sql("SELECT operation_id FROM identity.access_changes WHERE user_id=@id", Actor(first).UserId))!;
        Assert.Null(await app.Services.GetRequiredService<IRevocationStatusReader>().ReadAsync(operation, Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Accepted_revocation_resumes_after_the_Identity_host_restarts()
    {
        var auth = await Login(); var other = await Login(false); owner.Blocked = true;
        var response = await Authorized(HttpMethod.Delete, "/api/v1/auth/sessions/" + Actor(auth).SessionId, other);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        client.Dispose(); await app.DisposeAsync();
        await InitializeAsync(); // Same DB/Redis, a fresh coordinator and healthy owner adapters.
        await Wait(async () => (await Me(auth)).StatusCode == HttpStatusCode.Unauthorized);
        Assert.Equal(HttpStatusCode.OK, (await Me(other)).StatusCode);
    }

    [Fact]
    public async Task Redis_reader_failure_returns_503_for_HTTP_and_hub_without_falling_back_to_DB()
    {
        var auth = await Login();
        var isolated = new ConfigurationBuilder().AddConfiguration(settings).AddInMemoryCollection(new Dictionary<string, string?>
        { ["Authentication:Sessions:KeyPrefix"] = "scdc:auth:unavailable:" + Guid.NewGuid().ToString("N") + ":" }).Build();
        using var readConnection = new RedisSessionConnection(isolated, "Reader");
        await using var unavailable = original.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(database.Configuration));
            builder.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton<IAuthenticatedSessionReader>(
                new RedisSessionReader(readConnection, TimeProvider.System))));
        });
        using var http = unavailable.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());
        foreach (var path in new[] { "/api/v1/users/me", "/hubs/chat/negotiate?negotiateVersion=1" })
        {
            var response = path.StartsWith("/hubs", StringComparison.Ordinal) ? await http.PostAsync(path, null) : await http.GetAsync(path);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("ACCESS_CHECK_UNAVAILABLE", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task Resuming_an_accepted_Community_operation_rechecks_the_original_session_without_a_stored_JWT()
    {
        var auth = await Login(); var actor = Actor(auth) with { SecurityStamp = Guid.Empty };
        await using var scope = app.Services.CreateAsyncScope();
        var issuer = scope.ServiceProvider.GetRequiredKeyedService<IIdentityPermitIssuer>(AccessOwner.Community);
        var request = new PermitRequest(actor, IdentityPurpose.ChannelCreate, Guid.NewGuid(), Guid.NewGuid(), Context: IdentityAccessContext.ResumingAcceptedOperation);
        Assert.Equal(2, (await issuer.AcquireAsync(request, default)).Scopes.Count);
        await Assert.ThrowsAsync<AccessCheckException>(() => issuer.AcquireAsync(request with { Purpose = IdentityPurpose.DmSendNew }, default));
        await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = auth.GetProperty("refreshToken").GetString() });
        await Assert.ThrowsAsync<AccessCheckException>(() => issuer.AcquireAsync(request, default));
    }

    [Fact]
    public async Task Bootstrap_snapshot_matches_the_migration_checksum()
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(RedisAccessDatabase.Repo(), "database/postgres/migrations/" + IdentityAccessMigration.FileName));
        Assert.False(await IdentityAccessMigration.ApplyAsync(database.Admin, sql));
        await Assert.ThrowsAsync<MigrationPreflightException>(() => IdentityAccessMigration.ApplyAsync(database.Admin, sql + "\n-- drift"));
    }

    [Fact]
    public async Task Generated_ACL_allows_real_client_publication_and_reading_but_denies_reader_writes()
    {
        var auth = await Login();
        var directory = Directory.CreateTempSubdirectory("scdc-redis-acl-").FullName;
        using (var process = Process.Start(new ProcessStartInfo("python3")
        {
            ArgumentList = { Path.Combine(RedisAccessDatabase.Repo(), "scripts/init_redis_secrets.py"), directory },
            UseShellExecute = false
        })!)
        { await process.WaitForExitAsync(); Assert.Equal(0, process.ExitCode); }
        using var administrator = new RedisSessionConnection(settings, "Writer");
        var redis = administrator.Database;
        try
        {
            foreach (var line in await File.ReadAllLinesAsync(Path.Combine(directory, "users.acl")))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts[1] == "default") continue;
                await redis.ExecuteAsync("ACL", new object[] { "SETUSER", parts[1], "reset" }.Concat(parts.Skip(2).Cast<object>()).ToArray());
            }
            var secured = new ConfigurationBuilder().AddConfiguration(settings).AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Sessions:KeyPrefix"] = "scdc:auth:acl-bootstrap:" + Guid.NewGuid().ToString("N") + ":",
                ["Authentication:Sessions:ReaderUser"] = "scdc_reader",
                ["Authentication:Sessions:WriterUser"] = "scdc_writer",
                ["Authentication:Sessions:ReaderPasswordFile"] = Path.Combine(directory, "reader.password"),
                ["Authentication:Sessions:WriterPasswordFile"] = Path.Combine(directory, "writer.password")
            }).Build();
            using var publisher = new SessionPublisher(secured);
            await publisher.PublishUserAsync(Actor(auth).UserId, default);
            using var reader = new RedisSessionConnection(secured, "Reader");
            Assert.True((await new RedisSessionReader(reader, TimeProvider.System).ReadAsync(Actor(auth), default)).Allowed);
            var key = secured["Authentication:Sessions:KeyPrefix"] + "proof";
            await Assert.ThrowsAsync<RedisServerException>(() => reader.Database.HashSetAsync(key, "write", "denied"));
            await Assert.ThrowsAsync<RedisServerException>(() => reader.Database.ScriptEvaluateAsync("return redis.call('HSET',KEYS[1],'write','denied')", [key]));
        }
        finally { await redis.ExecuteAsync("ACL", "DELUSER", "scdc_reader", "scdc_writer"); }
    }

    public async Task DisposeAsync() { client.Dispose(); await app.DisposeAsync(); await original.DisposeAsync(); }
    private sealed class FaultingOwner(IIdentityFenceOwner inner) : IIdentityFenceOwner
    {
        public AccessOwner Owner => inner.Owner;
        public volatile bool Blocked;
        public bool LoseNextReceipt;
        public bool ReconstructReceipt;
        public bool CorruptReceipt;
        public TaskCompletionSource? Stall;
        public int Calls;
        public async Task<IdentityFenceReceipt> ApplyAsync(IdentityFenceBatch batch, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            if (Blocked) throw new AccessCheckException();
            if (Stall is { } stall) await stall.Task; // Deliberately ignore cancellation like a lost RPC reply.
            var result = await inner.ApplyAsync(batch, ct);
            if (LoseNextReceipt) { LoseNextReceipt = false; throw new AccessCheckException(); }
            if (CorruptReceipt) return result with { Batch = result.Batch with
                { Scopes = result.Batch.Scopes.Select(scope => scope with { Epoch = scope.Epoch + 1 }).ToArray() } };
            return ReconstructReceipt
                ? JsonSerializer.Deserialize<IdentityFenceReceipt>(JsonSerializer.Serialize(result))!
                : result;
        }
    }
}
