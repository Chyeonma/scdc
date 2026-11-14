using System.Security.Cryptography;

namespace SCDC.Shared.Core.Authentication;

/// <summary>Framework-independent trust configuration. ASP.NET adapters live in each service.</summary>
public sealed class JwtTrustOptions
{
    public const string SectionName = "Authentication:Jwt";
    public const string Algorithm = "RS256";
    public string Issuer { get; init; } = "SCDC";
    public string Audience { get; init; } = "SCDC.WebClient";
    public string PublicKeyDirectory { get; init; } = "";
    // ConfigurationBinder appends array values; keep this empty and configure the trusted IDs explicitly.
    public string[] KeyIds { get; init; } = [];
    public int ClockSkewSeconds { get; init; } = 30;

    public static bool IsValidKeyId(string value) => value.Length is >= 1 and <= 64
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');

    public IReadOnlyList<TrustedRsaKey> LoadPublicKeys()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience)
            || string.IsNullOrWhiteSpace(PublicKeyDirectory) || ClockSkewSeconds is < 0 or > 60
            || KeyIds.Length is < 1 or > 5 || KeyIds.Any(id => !IsValidKeyId(id))
            || KeyIds.Distinct(StringComparer.Ordinal).Count() != KeyIds.Length)
            throw new InvalidOperationException("JWT trust requires issuer, audience, public key directory, 1-5 unique safe key IDs and clock skew from 0 to 60 seconds.");

        return KeyIds.Select(id =>
        {
            var pem = File.ReadAllText(Path.Combine(PublicKeyDirectory, id + ".pub.pem"));
            if (!pem.StartsWith("-----BEGIN PUBLIC KEY-----", StringComparison.Ordinal)
                || pem.Contains("PRIVATE KEY", StringComparison.Ordinal))
                throw new InvalidOperationException("JWT consumers accept only SPKI public keys.");
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            if (rsa.KeySize is < 2048 or > 4096)
                throw new InvalidOperationException("JWT RSA keys must be 2048-4096 bits.");
            return new TrustedRsaKey(id, rsa.ExportParameters(false));
        }).ToArray();
    }
}

public sealed record TrustedRsaKey(string KeyId, RSAParameters Parameters);
