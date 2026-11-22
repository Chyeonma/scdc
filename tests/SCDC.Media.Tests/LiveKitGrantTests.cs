using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using SCDC.Media.Domain;
using SCDC.Media.Infrastructure;

namespace SCDC.Media.Tests;

public sealed class LiveKitGrantTests
{
    private static LiveKitProvider Provider(string secret = "test-only-secret-with-at-least-32-characters") => new(new HttpClient(),
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["LiveKit:ApiKey"] = "test", ["LiveKit:ApiSecret"] = secret }).Build(), TimeProvider.System);
    [Fact] public void Grant_is_short_lived_signed_and_bound_to_call_session_and_tab()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = new MediaActor(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "identity-token", new(Guid.NewGuid(), "u", "User"), now.AddMinutes(5));
        var call = new CallRecord(Guid.CreateVersion7(), actor.UserId, Guid.NewGuid(), actor.SessionId, actor.InstanceId,
            Guid.NewGuid(), Guid.NewGuid(), "A", "B", "active", null, 2, "test-room", now, now.AddSeconds(30), false, now);
        var provider = Provider(); var token = provider.Token(call, actor);
        Assert.True(provider.Validate(token, out var id, out var user, out var session, out var instance));
        Assert.Equal((call.Id, actor.UserId, actor.SessionId, actor.InstanceId), (id, user, session, instance));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.InRange(jwt.ValidTo, now.UtcDateTime.AddSeconds(58), now.UtcDateTime.AddSeconds(61));
        Assert.DoesNotContain("identity-token", token);
        Assert.False(Provider("different-test-key-with-at-least-32-characters").Validate(token, out _, out _, out _, out _));
        Assert.False(provider.Validate("unsigned.or.invalid", out _, out _, out _, out _));
    }
}
