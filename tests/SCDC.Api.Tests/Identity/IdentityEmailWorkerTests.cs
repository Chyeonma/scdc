using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SCDC.Api.Tests.Infrastructure;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure.Email;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Api.Tests.Identity;

[CollectionDefinition("Identity email", DisableParallelization = true)]
public sealed class IdentityEmailCollection;

[Collection("Identity email")]
public sealed class IdentityEmailWorkerTests(RedisAccessDatabase database) : IClassFixture<RedisAccessDatabase>, IAsyncLifetime
{
    private const string Password = "Initial123";
    private readonly TestClock _clock = new();
    private readonly RecordingSender _sender = new();
    private readonly SCDCWebApplicationFactory _baseFactory = new();
    private readonly List<Guid> _users = [];
    private WebApplicationFactory<ApiProgram> _factory = null!;
    private HttpClient _client = null!;

    private WebApplicationFactory<ApiProgram> CreateFactory() => _baseFactory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(database.Configuration.Concat(
            new Dictionary<string, string?>
            {
                ["Modules:Identity:ExposeDevelopmentTokens"] = "false",
                ["Modules:Identity:Email:Enabled"] = "true",
                ["Modules:Identity:Email:SenderAddress"] = "scdc@example.test",
                ["Modules:Identity:Email:AppPassword"] = "test-only",
                ["Modules:Identity:Email:BatchSize"] = "1",
                ["Modules:Identity:Email:SendTimeoutSeconds"] = "1"
            })));
        builder.ConfigureServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(_clock));
            services.Replace(ServiceDescriptor.Singleton<IAccountEmailSender>(_sender));
        });
    });

    public async Task InitializeAsync()
    {
        _factory = CreateFactory(); _client = _factory.CreateClient();
        await SCDCWebApplicationFactory.WaitForAccessReadyAsync(_client);
    }

    [Fact]
    public async Task Pending_account_can_reset_without_verifying_and_bad_password_does_not_consume_token()
    {
        var account = await RegisterAsync();
        var verification = await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail);
        var request = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { account.Email });
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        Assert.Null((await request.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("developmentResetToken").GetString());
        var reset = await TokenAsync(account.Id, AccountTokenPurpose.ResetPassword);
        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(reset, "abc")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await ResetAsync(reset, "Changed456")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(reset, "Changed456")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await LoginAsync(account, "Changed456")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await VerifyAsync(verification)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(account, "Changed456")).StatusCode);
    }

    [Fact]
    public async Task Resend_obeys_59_60_second_boundary_and_parallel_requests_issue_one_link()
    {
        var account = await RegisterAsync();
        var oldToken = await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail);
        _clock.Advance(TimeSpan.FromSeconds(59));
        Assert.Equal(HttpStatusCode.Accepted, (await ResendAsync(account.Email)).StatusCode);
        Assert.Equal(oldToken, await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail));
        _clock.Advance(TimeSpan.FromSeconds(1));
        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => ResendAsync(account.Email)));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Accepted, response.StatusCode));
        var newToken = await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail);
        Assert.NotEqual(oldToken, newToken);
        await WithDbAsync(async db => Assert.Equal(2, await db.AccountTokens.CountAsync(token => token.UserId == account.Id)));
        Assert.Equal(HttpStatusCode.BadRequest, (await VerifyAsync(oldToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await VerifyAsync(newToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await ResendAsync(account.Email)).StatusCode);
    }

    [Fact]
    public async Task Concurrent_verification_consumes_token_once()
    {
        var account = await RegisterAsync();
        var token = await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail);
        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => VerifyAsync(token)));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.NoContent);
        Assert.Equal(4, responses.Count(response => response.StatusCode == HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task Recovery_responses_match_for_unknown_cooldown_verified_and_unavailable_accounts()
    {
        var account = await RegisterAsync();
        const string accepted = "{\"accepted\":true}";
        Assert.Equal(accepted, await (await ResendAsync("unknown@example.test")).Content.ReadAsStringAsync());
        Assert.Equal(accepted, await (await ResendAsync(account.Email)).Content.ReadAsStringAsync());
        await VerifyAsync(await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail));
        Assert.Equal(accepted, await (await ResendAsync(account.Email)).Content.ReadAsStringAsync());
        await WithDbAsync(db => db.Users.Where(user => user.Id == account.Id).ExecuteUpdateAsync(setters => setters.SetProperty(user => user.Status, UserStatus.Suspended)));
        var unavailable = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { account.Email });
        var unknown = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "unknown@example.test" });
        Assert.Equal(HttpStatusCode.Accepted, unavailable.StatusCode);
        Assert.Equal(await unknown.Content.ReadAsStringAsync(), await unavailable.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Reset_cooldown_keeps_current_link_and_purposes_are_independent()
    {
        var account = await RegisterAsync();
        var verify = await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail);
        await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { account.Email });
        var oldReset = await TokenAsync(account.Id, AccountTokenPurpose.ResetPassword);
        await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { account.Email });
        Assert.Equal(oldReset, await TokenAsync(account.Id, AccountTokenPurpose.ResetPassword));
        _clock.Advance(TimeSpan.FromSeconds(60));
        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { account.Email })));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Accepted, response.StatusCode));
        Assert.NotEqual(oldReset, await TokenAsync(account.Id, AccountTokenPurpose.ResetPassword));
        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(oldReset, "Changed456")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await VerifyAsync(verify)).StatusCode);
        await WithDbAsync(async db => Assert.Equal(2, await db.AccountTokens.CountAsync(token => token.UserId == account.Id && token.Purpose == AccountTokenPurpose.ResetPassword)));
    }

    [Fact]
    public async Task Links_expire_at_exact_deadline()
    {
        var account = await RegisterAsync();
        var verify = await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail);
        await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { account.Email });
        var reset = await TokenAsync(account.Id, AccountTokenPurpose.ResetPassword);
        _clock.Advance(TimeSpan.FromMinutes(30));
        Assert.Equal(HttpStatusCode.BadRequest, (await VerifyAsync(verify)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(reset, "Changed456")).StatusCode);
    }

    [Fact]
    public async Task Worker_sends_protected_link_once_and_purges_envelope_after_acceptance()
    {
        var account = await RegisterAsync();
        var token = await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail);
        await WithDbAsync(async db =>
        {
            var delivery = await db.EmailDeliveries.SingleAsync(item => item.UserId == account.Id);
            Assert.DoesNotContain(token, delivery.ProtectedEnvelope!);
            var payload = (await db.OutboxEvents.SingleAsync(item => item.Id == delivery.OutboxEventId)).Payload;
            Assert.DoesNotContain(token, payload);
            Assert.DoesNotContain(account.Email, payload);
        });
        Assert.Equal(1, await ProcessAsync());
        Assert.Equal(0, await ProcessAsync());
        var sent = Assert.Single(_sender.Messages);
        Assert.Contains("http://localhost:3000/auth/verify#token=" + token, sent.Body);
        await WithDbAsync(async db =>
        {
            var delivery = await db.EmailDeliveries.SingleAsync(item => item.UserId == account.Id);
            Assert.Equal(EmailDeliveryStatus.ProviderAccepted, delivery.Status);
            Assert.Null(delivery.ProtectedEnvelope);
            Assert.Null(delivery.LeaseOwner);
            Assert.Equal(1, delivery.AttemptCount);
            Assert.NotNull((await db.OutboxEvents.SingleAsync(item => item.Id == delivery.OutboxEventId)).PublishedAt);
        });
        Assert.Equal(HttpStatusCode.NoContent, (await VerifyAsync(token)).StatusCode);
    }

    [Fact]
    public async Task Worker_retries_transient_failure_and_suppresses_replaced_links()
    {
        var account = await RegisterAsync();
        _sender.Result = new EmailSendResult(false, true, "Email.Smtp.421");
        Assert.Equal(1, await ProcessAsync());
        Assert.Equal(0, await ProcessAsync());
        _clock.Advance(TimeSpan.FromSeconds(60));
        await ResendAsync(account.Email);
        _sender.Result = new EmailSendResult(true, false);
        Assert.Equal(1, await ProcessAsync());
        Assert.Equal(2, _sender.Messages.Count);
        await WithDbAsync(async db =>
        {
            var deliveries = await db.EmailDeliveries.Where(item => item.UserId == account.Id).OrderBy(item => item.CreatedAt).ToListAsync();
            Assert.Equal(EmailDeliveryStatus.Suppressed, deliveries[0].Status);
            Assert.Null(deliveries[0].ProtectedEnvelope);
            Assert.Equal(EmailDeliveryStatus.ProviderAccepted, deliveries[1].Status);
        });
    }

    [Fact]
    public async Task Worker_recovers_expired_lease_and_retries_then_accepts()
    {
        var account = await RegisterAsync();
        await WithDbAsync(db => db.EmailDeliveries.Where(item => item.UserId == account.Id).ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Status, EmailDeliveryStatus.Sending)
            .SetProperty(item => item.LeaseOwner, Guid.NewGuid())
            .SetProperty(item => item.LeaseUntil, _clock.GetUtcNow().AddSeconds(30))));
        Assert.Equal(0, await ProcessAsync());
        _clock.Advance(TimeSpan.FromSeconds(30));
        _sender.Result = new EmailSendResult(false, true, "Email.Smtp.421");
        Assert.Equal(1, await ProcessAsync());
        _clock.Advance(TimeSpan.FromSeconds(14));
        _sender.Result = new EmailSendResult(true, false);
        Assert.Equal(1, await ProcessAsync());
        await WithDbAsync(async db => Assert.Equal(2, (await db.EmailDeliveries.SingleAsync(item => item.UserId == account.Id)).AttemptCount));
    }

    [Fact]
    public async Task Two_workers_cannot_send_one_delivery_at_the_same_time()
    {
        await RegisterAsync();
        _sender.Block = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = ProcessAsync();
        await _sender.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try { Assert.Equal(0, await ProcessAsync()); }
        finally { _sender.Block.SetResult(); }
        Assert.Equal(1, await first);
        Assert.Single(_sender.Messages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Worker_purges_consumed_or_expired_links_without_sending(bool expired)
    {
        var account = await RegisterAsync();
        if (expired) _clock.Advance(TimeSpan.FromMinutes(30));
        else await VerifyAsync(await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail));
        Assert.Equal(0, await ProcessAsync());
        Assert.Empty(_sender.Messages);
        await WithDbAsync(async db =>
        {
            var delivery = await db.EmailDeliveries.SingleAsync(item => item.UserId == account.Id);
            Assert.Equal(EmailDeliveryStatus.Suppressed, delivery.Status);
            Assert.Null(delivery.ProtectedEnvelope);
        });
    }

    [Fact]
    public async Task Worker_timeout_retries_but_permanent_failure_purges_envelope()
    {
        var account = await RegisterAsync();
        _sender.Block = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Assert.Equal(1, await ProcessAsync());
        await WithDbAsync(async db => Assert.Equal(EmailDeliveryStatus.RetryPending, (await db.EmailDeliveries.SingleAsync(item => item.UserId == account.Id)).Status));
        _sender.Block = null;
        _sender.Result = new EmailSendResult(false, false, "Email.AuthenticationFailed");
        _clock.Advance(TimeSpan.FromSeconds(14));
        Assert.Equal(1, await ProcessAsync());
        await WithDbAsync(async db =>
        {
            var delivery = await db.EmailDeliveries.SingleAsync(item => item.UserId == account.Id);
            Assert.Equal(EmailDeliveryStatus.Failed, delivery.Status);
            Assert.Null(delivery.ProtectedEnvelope);
        });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Worker_suppresses_links_when_account_or_primary_email_changes(bool reset, bool suspended)
    {
        var account = await RegisterAsync();
        if (reset)
        {
            await VerifyAsync(await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail));
            await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { account.Email });
        }
        await WithDbAsync(async db =>
        {
            if (suspended)
                await db.Users.Where(user => user.Id == account.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.Status, UserStatus.Suspended));
            else
                await db.UserEmails.Where(email => email.UserId == account.Id && email.IsPrimary)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(email => email.Email, $"changed-{account.Id:N}@example.test"));
        });
        Assert.Equal(0, await ProcessAsync());
        Assert.Empty(_sender.Messages);
        await WithDbAsync(async db => Assert.All(await db.EmailDeliveries.Where(item => item.UserId == account.Id).ToListAsync(),
            delivery =>
            {
                Assert.Equal(EmailDeliveryStatus.Suppressed, delivery.Status);
                Assert.Null(delivery.ProtectedEnvelope);
            }));
    }

    [Fact]
    public async Task Worker_stops_after_five_attempts_and_never_retries_expired_links()
    {
        var account = await RegisterAsync();
        _sender.Result = new EmailSendResult(false, true, "Email.Smtp.421");
        for (var attempt = 0; attempt < 5; attempt++)
        {
            Assert.Equal(1, await ProcessAsync());
            _clock.Advance(TimeSpan.FromSeconds(304));
        }
        Assert.Equal(0, await ProcessAsync());
        Assert.Equal(5, _sender.Messages.Count);
        await WithDbAsync(async db =>
        {
            var delivery = await db.EmailDeliveries.SingleAsync(item => item.UserId == account.Id);
            Assert.Equal(EmailDeliveryStatus.Failed, delivery.Status);
            Assert.Null(delivery.ProtectedEnvelope);
        });
    }

    [Fact]
    public async Task Rolled_back_delivery_is_never_sent()
    {
        var account = await RegisterAsync();
        await VerifyAsync(await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail));
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = await db.Users.SingleAsync(item => item.Id == account.Id);
        var rawToken = scope.ServiceProvider.GetRequiredService<SCDC.Modules.Identity.Infrastructure.Security.ITokenService>().CreateOpaqueToken();
        var token = new AccountToken
        {
            Id = Guid.CreateVersion7(), UserId = account.Id, User = user,
            Purpose = AccountTokenPurpose.ResetPassword, TokenHash = rawToken.Hash, TargetValue = account.Email,
            CreatedAt = _clock.GetUtcNow(), ExpiresAt = _clock.GetUtcNow().AddMinutes(30)
        };
        db.AccountTokens.Add(token);
        scope.ServiceProvider.GetRequiredService<AccountEmailQueue>().Enqueue(user, token, rawToken.Value, _clock.GetUtcNow());
        await db.SaveChangesAsync();
        Assert.Equal(0, await ProcessAsync());
        await transaction.RollbackAsync();
        Assert.Equal(0, await ProcessAsync());
        Assert.Empty(_sender.Messages);
    }

    [Fact]
    public async Task Restart_uses_persisted_envelope_keys_and_recovers_only_after_lease_expiry()
    {
        var account = await RegisterAsync();
        await WithDbAsync(db => db.EmailDeliveries.Where(item => item.UserId == account.Id).ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Status, EmailDeliveryStatus.Sending)
            .SetProperty(item => item.LeaseOwner, Guid.NewGuid())
            .SetProperty(item => item.LeaseUntil, _clock.GetUtcNow().AddSeconds(30))));
        _client.Dispose(); await _factory.DisposeAsync();
        _factory = CreateFactory(); _client = _factory.CreateClient();
        await SCDCWebApplicationFactory.WaitForAccessReadyAsync(_client);
        Assert.Equal(0, await ProcessAsync());
        _clock.Advance(TimeSpan.FromSeconds(31));
        Assert.Equal(1, await ProcessAsync());
        Assert.Single(_sender.Messages);
        await WithDbAsync(async db => Assert.Equal(EmailDeliveryStatus.ProviderAccepted,
            (await db.EmailDeliveries.SingleAsync(item => item.UserId == account.Id)).Status));
    }

    [Fact]
    public async Task Stale_sender_cannot_overwrite_a_reclaimed_delivery_or_outbox_attempt_count()
    {
        var account = await RegisterAsync();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _sender.Block = release;
        var first = ProcessAsync();
        await _sender.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            _clock.Advance(TimeSpan.FromSeconds(31));
            _sender.Block = null;
            Assert.Equal(1, await ProcessAsync());
        }
        finally { release.TrySetResult(); }
        await first;
        await WithDbAsync(async db =>
        {
            var delivery = await db.EmailDeliveries.SingleAsync(item => item.UserId == account.Id);
            Assert.Equal(EmailDeliveryStatus.ProviderAccepted, delivery.Status);
            Assert.Equal(2, delivery.AttemptCount);
            Assert.Equal(2, (await db.OutboxEvents.SingleAsync(item => item.Id == delivery.OutboxEventId)).AttemptCount);
        });
    }

    [Fact]
    public async Task Independent_Identity_host_runs_worker_and_email_link_verifies_without_development_tokens()
    {
        var sender = new RecordingSender();
        var host = new IdentityNetworkFixture { EmailSender = sender };
        try
        {
            await host.InitializeAsync();
            var username = "email_host_" + Guid.NewGuid().ToString("N")[..12];
            var created = await host.Post(host.Identity, "/api/v1/auth/register", new
            {
                username, displayName = "Email host", email = username + "@example.test", password = Password
            }, null, 201);
            Assert.Null(created!["developmentVerificationToken"]);
            await sender.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var message = Assert.Single(sender.Messages);
            var link = message.Body.Split("\r\n").Single(line => line.StartsWith("http://", StringComparison.Ordinal));
            var token = Uri.UnescapeDataString(new Uri(link).Fragment[7..]);
            await host.Post(host.Identity, "/api/v1/auth/verify-email", new { token }, null, 204);
            await host.Post(host.Identity, "/api/v1/auth/login", new { login = username, password = Password }, null, 200);
        }
        finally { await host.DisposeAsync(); }
    }

    [Fact]
    public async Task Concurrent_registration_commits_one_user_token_policy_delivery_and_outbox()
    {
        var username = "email_race_" + Guid.NewGuid().ToString("N")[..12];
        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
            _client.PostAsJsonAsync("/api/v1/auth/register", new
            { username, displayName = "Email race", email = username + "@example.test", password = Password })));
        try
        {
            var success = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
            Assert.Equal(3, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
            var body = await success.Content.ReadFromJsonAsync<JsonElement>();
            var id = body.GetProperty("userId").GetGuid(); _users.Add(id);
            await WithDbAsync(async db =>
            {
                Assert.Equal(1, await db.AccountTokens.CountAsync(item => item.UserId == id));
                Assert.Equal(1, await db.AccountTokenPolicies.CountAsync(item => item.UserId == id));
                Assert.Equal(1, await db.EmailDeliveries.CountAsync(item => item.UserId == id));
                Assert.Equal(1, await db.OutboxEvents.CountAsync(item => item.AggregateId == id));
            });
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [Fact]
    public async Task Worker_does_not_start_a_send_without_enough_token_lifetime_for_its_timeout()
    {
        var account = await RegisterAsync();
        _clock.Advance(TimeSpan.FromMinutes(30) - TimeSpan.FromMilliseconds(500));
        Assert.Equal(0, await ProcessAsync());
        Assert.Empty(_sender.Messages);
        await WithDbAsync(async db =>
        {
            var delivery = await db.EmailDeliveries.SingleAsync(item => item.UserId == account.Id);
            Assert.Equal(EmailDeliveryStatus.Suppressed, delivery.Status);
            Assert.Equal(0, delivery.AttemptCount);
            Assert.Null(delivery.ProtectedEnvelope);
        });
    }

    [Fact]
    public async Task Encrypted_envelope_cannot_be_reassigned_to_a_different_delivery_or_recipient()
    {
        var first = await RegisterAsync();
        _clock.Advance(TimeSpan.FromSeconds(1));
        var second = await RegisterAsync();
        await WithDbAsync(async db =>
        {
            var envelope = await db.EmailDeliveries.Where(item => item.UserId == second.Id)
                .Select(item => item.ProtectedEnvelope).SingleAsync();
            await db.EmailDeliveries.Where(item => item.UserId == first.Id).ExecuteUpdateAsync(setters =>
                setters.SetProperty(item => item.ProtectedEnvelope, envelope));
        });
        Assert.Equal(1, await ProcessAsync());
        Assert.Empty(_sender.Messages);
        await WithDbAsync(async db =>
        {
            var delivery = await db.EmailDeliveries.SingleAsync(item => item.UserId == first.Id);
            Assert.Equal(EmailDeliveryStatus.Failed, delivery.Status);
            Assert.Equal("Email.EnvelopeUnavailable", delivery.LastErrorCode);
        });
        Assert.Equal(1, await ProcessAsync());
        Assert.Equal(second.Email, Assert.Single(_sender.Messages).Recipient);
    }

    private async Task<Account> RegisterAsync()
    {
        var username = "complete_" + Guid.NewGuid().ToString("N")[..12];
        var email = username + "@example.test";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new { username, email, displayName = "Identity completion", password = Password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Null(body.GetProperty("developmentVerificationToken").GetString());
        var id = body.GetProperty("userId").GetGuid();
        _users.Add(id);
        return new Account(id, username, email);
    }

    private Task<HttpResponseMessage> VerifyAsync(string token) => _client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token });
    private Task<HttpResponseMessage> ResetAsync(string token, string newPassword) => _client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token, newPassword });
    private Task<HttpResponseMessage> ResendAsync(string email) => _client.PostAsJsonAsync("/api/v1/auth/resend-verification", new { email });
    private Task<HttpResponseMessage> LoginAsync(Account account, string password = Password) => _client.PostAsJsonAsync("/api/v1/auth/login", new { login = account.Email, password });
    private async Task<JsonElement> AuthAsync(Account account) => await (await LoginAsync(account)).Content.ReadFromJsonAsync<JsonElement>();

    private async Task<HttpResponseMessage> AuthorizedAsync(HttpMethod method, string path, JsonElement auth, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());
        if (body is not null) request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private async Task<string> TokenAsync(Guid userId, AccountTokenPurpose purpose)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var delivery = await db.EmailDeliveries.Where(item => item.UserId == userId && item.Purpose == purpose)
            .OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id).FirstAsync();
        return scope.ServiceProvider.GetRequiredService<AccountEmailQueue>().Unprotect(delivery);
    }

    private async Task WithDbAsync(Func<IdentityDbContext, Task> action)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<IdentityDbContext>());
    }

    private async Task<int> ProcessAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IdentityEmailProcessor>().ProcessBatchAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _baseFactory.DisposeAsync();
        if (_users.Count > 0)
        {
            var ids = string.Join(',', _users.Select(id => "'" + id + "'"));
            await database.Sql($"DELETE FROM identity.session_publications WHERE user_id IN ({ids}); DELETE FROM identity.security_events WHERE user_id IN ({ids}); DELETE FROM identity.outbox_events WHERE aggregate_id IN ({ids}); DELETE FROM identity.users WHERE id IN ({ids});");
        }
    }

    private sealed record Account(Guid Id, string Username, string Email);
    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset _now = new(DateTimeOffset.UtcNow.Ticks / 10 * 10, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan span) => _now += span;
        public void Set(DateTimeOffset instant) => _now = instant;
    }
    private sealed class RecordingSender : IAccountEmailSender
    {
        public List<AccountEmail> Messages { get; } = [];
        public EmailSendResult Result { get; set; } = new(true, false);
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource? Block { get; set; }
        public async Task<EmailSendResult> SendAsync(AccountEmail email, CancellationToken cancellationToken)
        {
            Messages.Add(email);
            Started.TrySetResult();
            if (Block is not null) await Block.Task.WaitAsync(cancellationToken);
            return Result;
        }
    }
}
