using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SCDC.Media.Application;
using SCDC.Media.Domain;

namespace SCDC.Media.Infrastructure;

public sealed class LiveKitProvider(HttpClient client, IConfiguration config, TimeProvider clock) : IMediaProvider
{
    private string Key => config["LiveKit:ApiKey"] ?? throw new InvalidOperationException("LiveKit API key required.");
    private SymmetricSecurityKey Secret => new(Encoding.UTF8.GetBytes(config["LiveKit:ApiSecret"] ?? throw new InvalidOperationException("LiveKit secret required.")));
    private string Sign(Dictionary<string, object> claims, DateTimeOffset expires)
    {
        var now = clock.GetUtcNow();
        // IdentityModel accepts JSON elements for structured grants, not arbitrary CLR objects.
        claims["video"] = JsonSerializer.SerializeToElement(claims["video"]);
        var descriptor = new SecurityTokenDescriptor { Issuer = Key, Claims = claims,
            NotBefore = now.UtcDateTime.AddSeconds(-5), IssuedAt = now.UtcDateTime, Expires = expires.UtcDateTime,
            SigningCredentials = new(Secret, SecurityAlgorithms.HmacSha256) };
        return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
    }
    public string Token(CallRecord call, MediaActor actor) => Sign(new()
    {
        ["sub"] = Identity(call.Id, actor.UserId, actor.InstanceId), ["jti"] = Guid.NewGuid().ToString(),
        ["callId"] = call.Id.ToString(), ["actorId"] = actor.UserId.ToString(),
        ["sessionId"] = actor.SessionId.ToString(), ["instanceId"] = actor.InstanceId.ToString(),
        ["video"] = new { room = call.RoomName, roomJoin = true, canSubscribe = true,
            canPublish = true, canPublishData = false, canUpdateOwnMetadata = false,
            canPublishSources = new[] { "microphone", "camera", "screen_share", "screen_share_audio" } }
    }, clock.GetUtcNow().AddSeconds(60) < actor.ExpiresAt ? clock.GetUtcNow().AddSeconds(60) : actor.ExpiresAt);
    public static string Identity(Guid call, Guid user, Guid instance) => $"{call:N}-{user:N}-{instance:N}";
    public bool Validate(string token, out Guid callId, out Guid userId, out Guid sessionId, out Guid instanceId)
    {
        callId = userId = sessionId = instanceId = Guid.Empty;
        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = handler.ValidateToken(token, new TokenValidationParameters {
                ValidateIssuer = true, ValidIssuer = Key, ValidateAudience = false, ValidateIssuerSigningKey = true,
                IssuerSigningKey = Secret, ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ValidateLifetime = true, RequireExpirationTime = true, ClockSkew = TimeSpan.Zero
            }, out _);
            return Guid.TryParse(principal.FindFirst("callId")?.Value, out callId)
                && Guid.TryParse(principal.FindFirst("actorId")?.Value, out userId)
                && Guid.TryParse(principal.FindFirst("sessionId")?.Value, out sessionId)
                && Guid.TryParse(principal.FindFirst("instanceId")?.Value, out instanceId)
                && principal.FindFirst("sub")?.Value == Identity(callId, userId, instanceId);
        }
        catch (Exception e) when (e is SecurityTokenException or ArgumentException) { return false; }
    }
    public Task CreateAsync(CallRecord call, CancellationToken ct) => Send("CreateRoom", new { name = call.RoomName, max_participants = 2, empty_timeout = 60, departure_timeout = 5 }, ct);
    public Task DeleteAsync(CallRecord call, CancellationToken ct) => Send("DeleteRoom", new { room = call.RoomName }, ct);
    private async Task Send(string method, object body, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "twirp/livekit.RoomService/" + method) {
                Content = JsonContent.Create(body)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Sign(new() { ["video"] = new { roomCreate = true } }, clock.GetUtcNow().AddSeconds(30)));
            using var response = await client.SendAsync(request, ct);
            if (method == "DeleteRoom" && (int)response.StatusCode == 404) return;
            if (!response.IsSuccessStatusCode) throw new MediaProblem(503, "Media.ProviderUnavailable", "LiveKit could not confirm the operation.");
        }
        catch (HttpRequestException) { throw new MediaProblem(503, "Media.ProviderUnavailable", "LiveKit is unavailable."); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new MediaProblem(503, "Media.ProviderUnavailable", "LiveKit timed out."); }
    }
}
