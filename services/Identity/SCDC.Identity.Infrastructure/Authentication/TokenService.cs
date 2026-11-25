using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SCDC.Modules.Identity.Domain;
using SCDC.Shared.Core.Authentication;

namespace SCDC.Modules.Identity.Infrastructure.Security;

internal interface ITokenService
{
    IssuedAccessToken CreateAccessToken(
        User user,
        string displayName,
        Guid sessionId,
        Guid securityStamp);

    OpaqueToken CreateOpaqueToken();

    string HashOpaqueToken(string token);
}

internal sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAt);

internal sealed record OpaqueToken(string Value, string Hash);

internal sealed class TokenService(
    IOptions<IdentityOptions> options,
    JwtTrustOptions trust,
    IdentitySigningKey signingKey,
    TimeProvider timeProvider) : ITokenService
{
    private readonly IdentityOptions _options = options.Value;

    public IssuedAccessToken CreateAccessToken(
        User user,
        string displayName,
        Guid sessionId,
        Guid securityStamp)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Name, displayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new Claim("sid", sessionId.ToString()),
            new Claim("sst", securityStamp.ToString())
        };
        var token = new JwtSecurityToken(
            trust.Issuer,
            trust.Audience,
            claims,
            now.UtcDateTime,
            expiresAt.UtcDateTime,
            signingKey.Credentials);

        return new IssuedAccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public OpaqueToken CreateOpaqueToken()
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return new OpaqueToken(value, HashOpaqueToken(value));
    }

    public string HashOpaqueToken(string token) => Convert
        .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))
        .ToLowerInvariant();
}
