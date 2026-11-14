using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using SCDC.Shared.Core.Authentication;

namespace SCDC.Sample.Api.Authentication;

internal static class JwtAuthentication
{
    public static void AddSampleAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<JwtTrustOptions>()
            .Bind(builder.Configuration.GetSection(JwtTrustOptions.SectionName))
            .Validate(options => { _ = options.LoadPublicKeys(); return true; }, "Invalid JWT trust.")
            .ValidateOnStart();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtTrustOptions>>((options, configured) =>
        {
            var trust = configured.Value;
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = trust.Issuer,
                ValidateAudience = true, ValidAudience = trust.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = trust.LoadPublicKeys().Select(key => new RsaSecurityKey(key.Parameters) { KeyId = key.KeyId }),
                ValidAlgorithms = [JwtTrustOptions.Algorithm],
                TryAllIssuerSigningKeys = false,
                ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
                ClockSkew = TimeSpan.FromSeconds(trust.ClockSkewSeconds), NameClaimType = "name"
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (!new[] { "sub", "sid", "sst" }.All(claim =>
                            Guid.TryParse(context.Principal?.FindFirst(claim)?.Value, out _)))
                        context.Fail("Required identity claims are missing.");
                    return Task.CompletedTask;
                }
            };
        });
        builder.Services.AddAuthorization();
    }
}
