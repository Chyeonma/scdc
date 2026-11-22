using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SCDC.Api.Tests.Infrastructure;
using SCDC.Shared.Core.Authentication;

namespace SCDC.Api.Tests.Authentication;

public sealed class SharedJwtTests(SampleWebApplicationFactory sample) : IClassFixture<SampleWebApplicationFactory>
{
    private static string Token(string variant = "valid", string? keyId = null, RSA? signingKey = null)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(TestJwtKeys.PrivateKeyPath));
        var key = new RsaSecurityKey((signingKey ?? rsa).ExportParameters(true))
            { KeyId = keyId ?? TestJwtKeys.KeyId };
        var now = DateTime.UtcNow;
        var claims = new[] { "sub", "sid", "sst" }.Select(name => new Claim(name, Guid.NewGuid().ToString())).ToList();
        if (variant == "missing-claims") claims.RemoveAt(1);
        SigningCredentials? credentials = variant switch
        {
            "unsigned" => null,
            "hs256" => new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                File.ReadAllText(Path.Combine(TestJwtKeys.PublicKeyDirectory, TestJwtKeys.KeyId + ".pub.pem")))), SecurityAlgorithms.HmacSha256),
            "rs512" => new SigningCredentials(key, SecurityAlgorithms.RsaSha512),
            _ => new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
        };
        var token = new JwtSecurityToken(variant == "issuer" ? "untrusted" : "SCDC",
            variant == "audience" ? "untrusted" : "SCDC.WebClient", claims,
            variant == "future" ? now.AddMinutes(2) : now.AddMinutes(-5),
            variant == "no-expiry" ? null : variant == "expired" ? now.AddMinutes(-2) : now.AddMinutes(5), credentials);
        if (variant == "no-kid") token.Header.Remove("kid");
        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        if (variant == "tampered")
        {
            var parts = encoded.Split('.');
            parts[1] = Base64UrlEncoder.Encode(Base64UrlEncoder.Decode(parts[1]).Replace("SCDC.WebClient", "tampered"));
            encoded = string.Join('.', parts);
        }
        return encoded;
    }

    private static async Task<HttpResponseMessage> Get(HttpClient client, string? token = null, string path = "/sample/protected")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Health_is_public_and_protected_endpoint_requires_a_bearer_header()
    {
        using var client = sample.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        using var denied = await Get(client);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        Assert.Contains(denied.Headers.WwwAuthenticate, value => value.Scheme == "Bearer");
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get(client, path: "/sample/protected?access_token=" + Token())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Get(client, Token())).StatusCode);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("tampered")]
    [InlineData("missing-claims")]
    [InlineData("no-expiry")]
    [InlineData("no-kid")]
    [InlineData("unsigned")]
    [InlineData("hs256")]
    [InlineData("rs512")]
    public async Task Invalid_tokens_are_rejected(string variant)
    {
        using var client = sample.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get(client, Token(variant))).StatusCode);
    }

    [Fact]
    public async Task Unknown_key_and_wrong_signature_are_rejected()
    {
        using var other = RSA.Create(2048);
        using var client = sample.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get(client, Token(signingKey: other))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get(client, Token(keyId: "unknown"))).StatusCode);
    }

    [Fact]
    public async Task Rotation_accepts_both_explicitly_trusted_keys()
    {
        using var other = RSA.Create(2048);
        const string rotatedId = "test-rotated-v2";
        File.WriteAllText(Path.Combine(TestJwtKeys.PublicKeyDirectory, rotatedId + ".pub.pem"), other.ExportSubjectPublicKeyInfoPem());
        using var rotated = sample.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:Jwt:KeyIds:1"] = rotatedId })));
        using var client = rotated.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await Get(client, Token())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Get(client, Token(keyId: rotatedId, signingKey: other))).StatusCode);
    }

    [Fact]
    public async Task Identity_issued_token_is_accepted_by_the_independent_sample()
    {
        await using var identity = new SCDCWebApplicationFactory();
        using var client = identity.CreateClient();
        var connection = identity.Services.GetRequiredService<IConfiguration>().GetConnectionString("TestDatabase")!;
        Assert.EndsWith("_test", new NpgsqlConnectionStringBuilder(connection).Database);
        var name = "jwt_" + Guid.NewGuid().ToString("N")[..16];
        var registered = await client.PostAsJsonAsync("/api/v1/auth/register", new
            { username = name, displayName = "JWT test", email = name + "@example.test", password = "Initial123" });
        registered.EnsureSuccessStatusCode();
        var account = await registered.Content.ReadFromJsonAsync<JsonElement>();
        try
        {
            (await client.PostAsJsonAsync("/api/v1/auth/verify-email", new
                { token = account.GetProperty("developmentVerificationToken").GetString() })).EnsureSuccessStatusCode();
            var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { login = name, password = "Initial123" });
            login.EnsureSuccessStatusCode();
            var auth = await login.Content.ReadFromJsonAsync<JsonElement>();
            var token = auth.GetProperty("accessToken").GetString()!;
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            Assert.Equal("RS256", jwt.Header.Alg);
            Assert.Equal(TestJwtKeys.KeyId, jwt.Header.Kid);
            using var sampleClient = sample.CreateClient();
            using var accepted = await Get(sampleClient, token);
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
            Assert.Equal("no-store", accepted.Headers.CacheControl?.ToString());
            var body = await accepted.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(account.GetProperty("userId").GetGuid().ToString(), body.GetProperty("userId").GetString());
        }
        finally
        {
            await using var db = new NpgsqlConnection(connection);
            await db.OpenAsync();
            await using var delete = new NpgsqlCommand("DELETE FROM identity.users WHERE id=@id", db);
            delete.Parameters.AddWithValue("id", account.GetProperty("userId").GetGuid());
            await delete.ExecuteNonQueryAsync();
        }
    }

    [Theory]
    [InlineData("../private")]
    [InlineData("a/b")]
    [InlineData("")]
    public void Unsafe_key_ids_are_rejected(string id)
    {
        var trust = new JwtTrustOptions { PublicKeyDirectory = TestJwtKeys.PublicKeyDirectory, KeyIds = [id] };
        Assert.Throws<InvalidOperationException>(() => trust.LoadPublicKeys());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Consumers_reject_private_or_weak_public_keys(bool privateKey)
    {
        var folder = Directory.CreateTempSubdirectory("scdc-invalid-trust-");
        try
        {
            using var rsa = RSA.Create(privateKey ? 2048 : 1024);
            File.WriteAllText(Path.Combine(folder.FullName, "test.pub.pem"), privateKey
                ? rsa.ExportPkcs8PrivateKeyPem() : rsa.ExportSubjectPublicKeyInfoPem());
            var trust = new JwtTrustOptions { PublicKeyDirectory = folder.FullName, KeyIds = ["test"] };
            Assert.Throws<InvalidOperationException>(() => trust.LoadPublicKeys());
        }
        finally { folder.Delete(true); }
    }
}
