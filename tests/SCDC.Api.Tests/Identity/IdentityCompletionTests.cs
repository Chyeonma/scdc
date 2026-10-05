using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

[CollectionDefinition("Identity completion", DisableParallelization = true)]
public sealed class IdentityCompletionCollection;

[Collection("Identity completion")]
public sealed class IdentityCompletionTests : IAsyncLifetime
{
    private const string Password = "Initial123";
    private readonly TestClock _clock = new();
    private readonly RecordingSender _sender = new();
    private readonly SCDCWebApplicationFactory _baseFactory = new();
    private readonly List<Guid> _users = [];
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Modules:Identity:ExposeDevelopmentTokens"] = "false",
                    ["Modules:Identity:Email:Enabled"] = "true",
                    ["Modules:Identity:Email:SenderAddress"] = "scdc@example.test",
                    ["Modules:Identity:Email:AppPassword"] = "test-only",
                    ["Modules:Identity:Email:BatchSize"] = "1",
                    ["Modules:Identity:Email:SendTimeoutSeconds"] = "1"
                }));
            builder.ConfigureServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton<TimeProvider>(_clock));
                services.Replace(ServiceDescriptor.Singleton<IAccountEmailSender>(_sender));
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters.LifetimeValidator = (notBefore, expires, _, parameters) =>
                        expires is not null && _clock.GetUtcNow().UtcDateTime <= expires.Value + parameters.ClockSkew
                        && (notBefore is null || _clock.GetUtcNow().UtcDateTime >= notBefore.Value - parameters.ClockSkew);
                });
            });
        });
        _client = _factory.CreateClient();
        return Task.CompletedTask;
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
    public async Task Change_rejects_same_password_but_reset_accepts_it_and_revokes_all_sessions()
    {
        var account = await RegisterAsync();
        await VerifyAsync(await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail));
        var first = await AuthAsync(account);
        var second = await AuthAsync(account);
        await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { account.Email });
        var reset = await TokenAsync(account.Id, AccountTokenPurpose.ResetPassword);
        var change = await AuthorizedAsync(HttpMethod.Post, "/api/v1/auth/change-password", first,
            new { currentPassword = Password, newPassword = Password });
        Assert.Equal(HttpStatusCode.BadRequest, change.StatusCode);
        Assert.Equal("Identity.PasswordUnchanged", (await change.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());
        Assert.Equal(HttpStatusCode.OK, (await AuthorizedAsync(HttpMethod.Get, "/api/v1/users/me", first)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await ResetAsync(reset, Password)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthorizedAsync(HttpMethod.Get, "/api/v1/users/me", first)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthorizedAsync(HttpMethod.Get, "/api/v1/users/me", second)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(reset, Password)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(account)).StatusCode);
    }

    [Theory]
    [InlineData(32, HttpStatusCode.Created)]
    [InlineData(33, HttpStatusCode.BadRequest)]
    public async Task Registration_normalizes_before_validation_and_counts_utf16(int emojis, HttpStatusCode expected)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var username = $"norm_{suffix}";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = $"  {username}  ", displayName = "  " + string.Concat(Enumerable.Repeat("😀", emojis)) + "  ",
            email = $"  {username.ToUpperInvariant()}@EXAMPLE.TEST  ", password = Password
        });
        Assert.Equal(expected, response.StatusCode);
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var id = body.GetProperty("userId").GetGuid();
            _users.Add(id);
            Assert.Equal(username + "@example.test", body.GetProperty("email").GetString());
            await VerifyAsync(await TokenAsync(id, AccountTokenPurpose.VerifyEmail));
            Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/v1/auth/login", new { login = "  " + username.ToUpperInvariant() + "@EXAMPLE.TEST  ", password = Password })).StatusCode);
        }
    }

    [Fact]
    public async Task Database_rejects_display_names_above_utf16_limit()
    {
        var account = await RegisterAsync();
        await WithDbAsync(async db =>
        {
            var invalid = string.Concat(Enumerable.Repeat("😀", 33));
            var error = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE identity.user_profiles SET display_name = {invalid} WHERE user_id = {account.Id}"));
            Assert.Equal(Npgsql.PostgresErrorCodes.CheckViolation, error.SqlState);
        });
    }

    [Fact]
    public async Task Concurrent_duplicate_registrations_do_not_leave_partial_accounts()
    {
        var account = new Account(Guid.Empty, "duplicate_" + Guid.NewGuid().ToString("N")[..12], "");
        var email = account.Username + "@example.test";
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => _client.PostAsJsonAsync("/api/v1/auth/register", new { username = account.Username, email, displayName = "Duplicate", password = Password })));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        var body = await responses.Single(response => response.IsSuccessStatusCode).Content.ReadFromJsonAsync<JsonElement>();
        _users.Add(body.GetProperty("userId").GetGuid());
        await WithDbAsync(async db =>
        {
            Assert.Equal(1, await db.Users.CountAsync(user => user.Username == account.Username));
            Assert.Equal(1, await db.EmailDeliveries.CountAsync(delivery => delivery.UserId == _users[0]));
        });
    }

    [Fact]
    public async Task Access_expiry_and_absolute_session_expiry_are_enforced()
    {
        var account = await RegisterAsync();
        await VerifyAsync(await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail));
        var auth = await AuthAsync(account);
        var expires = auth.GetProperty("refreshTokenExpiresAt").GetDateTimeOffset();
        _clock.Advance(TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(31));
        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthorizedAsync(HttpMethod.Get, "/api/v1/users/me", auth)).StatusCode);
        _clock.Set(expires.AddSeconds(-1));
        var refreshed = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = auth.GetProperty("refreshToken").GetString() });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var next = await refreshed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expires, next.GetProperty("refreshTokenExpiresAt").GetDateTimeOffset());
        _clock.Set(expires);
        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthorizedAsync(HttpMethod.Get, "/api/v1/users/me", next)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = next.GetProperty("refreshToken").GetString() })).StatusCode);
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
    public async Task Profile_fields_are_normalized_persisted_and_identity_is_read_only()
    {
        var account = await RegisterAsync();
        await VerifyAsync(await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail));
        var auth = await AuthAsync(account);
        var response = await AuthorizedAsync(HttpMethod.Patch, "/api/v1/users/me", auth,
            new { displayName = "  Tên mới  ", bio = "  Giới thiệu  ", locale = "  en-US  ", timezone = "  UTC  ", username = "changed", email = "changed@example.test" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await (await AuthorizedAsync(HttpMethod.Get, "/api/v1/users/me", auth)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Tên mới", profile.GetProperty("displayName").GetString());
        Assert.Equal("Giới thiệu", profile.GetProperty("bio").GetString());
        Assert.Equal("en-US", profile.GetProperty("locale").GetString());
        Assert.Equal("UTC", profile.GetProperty("timezone").GetString());
        Assert.Equal(account.Username, profile.GetProperty("username").GetString());
        Assert.Equal(account.Email, profile.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Revocation_respects_ownership_and_preserves_other_sessions()
    {
        var account = await RegisterAsync();
        var peer = await RegisterAsync();
        await VerifyAsync(await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail));
        await VerifyAsync(await TokenAsync(peer.Id, AccountTokenPurpose.VerifyEmail));
        var first = await AuthAsync(account);
        var second = await AuthAsync(account);
        var peerAuth = await AuthAsync(peer);
        var list = await (await AuthorizedAsync(HttpMethod.Get, "/api/v1/auth/sessions", second)).Content.ReadFromJsonAsync<JsonElement>();
        var secondId = list.EnumerateArray().Single(item => item.GetProperty("isCurrent").GetBoolean()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await AuthorizedAsync(HttpMethod.Delete, $"/api/v1/auth/sessions/{secondId}", peerAuth)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await AuthorizedAsync(HttpMethod.Delete, $"/api/v1/auth/sessions/{secondId}", first)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AuthorizedAsync(HttpMethod.Get, "/api/v1/users/me", first)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthorizedAsync(HttpMethod.Get, "/api/v1/users/me", second)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = new string('a', 64) })).StatusCode);
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
    public async Task Maintenance_keeps_active_refresh_family_and_terminal_session_deny_marker()
    {
        var account = await RegisterAsync();
        await VerifyAsync(await TokenAsync(account.Id, AccountTokenPurpose.VerifyEmail));
        var active = await AuthAsync(account);
        await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = active.GetProperty("refreshToken").GetString() });
        var terminal = await AuthAsync(account);
        await _client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = terminal.GetProperty("refreshToken").GetString() });
        _clock.Advance(TimeSpan.FromDays(8));
        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IdentityMaintenance>().RunAsync(CancellationToken.None);
        await WithDbAsync(async db =>
        {
            Assert.Equal(2, await db.RefreshTokens.CountAsync(token => token.Session.UserId == account.Id));
            var terminalSession = await db.AuthSessions.SingleAsync(session => session.UserId == account.Id && session.RevokedAt != null);
            Assert.Null(terminalSession.DeviceName);
            Assert.Null(terminalSession.UserAgent);
            Assert.NotNull(terminalSession.RevokedAt);
            Assert.Empty(await db.AccountTokens.Where(token => token.UserId == account.Id).ToListAsync());
            Assert.Equal(_clock.GetUtcNow().AddDays(-8), (await db.AccountTokenPolicies.SingleAsync(policy => policy.UserId == account.Id)).LastIssuedAt);
        });
        var reuse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = active.GetProperty("refreshToken").GetString() });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Equal("Identity.RefreshTokenReuseDetected", (await reuse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());
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
        return scope.ServiceProvider.GetRequiredService<AccountEmailQueue>().Unprotect(delivery.ProtectedEnvelope!);
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
        if (_factory is not null)
        {
            await WithDbAsync(async db =>
            {
                await db.OutboxEvents.Where(item => _users.Contains(item.AggregateId)).ExecuteDeleteAsync();
                await db.SecurityEvents.Where(item => item.UserId != null && _users.Contains(item.UserId.Value)).ExecuteDeleteAsync();
                await db.Users.Where(item => _users.Contains(item.Id)).ExecuteDeleteAsync();
            });
            _client.Dispose();
            await _factory.DisposeAsync();
        }
        await _baseFactory.DisposeAsync();
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
