using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SCDC.Modules.Community.Infrastructure.Authentication;
using SCDC.Contracts.Identity;
using SCDC.Shared.Core.Authentication;

namespace SCDC.Community.Api.Authentication;

internal static class ConsumerAuthentication
{
    public static void AddConsumerAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtTrustOptions>().Bind(configuration.GetSection(JwtTrustOptions.SectionName))
            .Validate(options => { _ = options.LoadPublicKeys(); return true; }, "Invalid JWT trust.").ValidateOnStart();
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<JwtTrustOptions>>().Value);
        services.AddSingleton(provider => new RedisSessionConnection(provider.GetRequiredService<IConfiguration>(), "Reader"));
        services.AddSingleton<RedisSessionReader>();
        services.AddSingleton<IAuthenticatedSessionReader>(provider => provider.GetRequiredService<RedisSessionReader>());
        services.AddSingleton<ISessionStoreReadiness>(provider => provider.GetRequiredService<RedisSessionReader>());
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<JwtTrustOptions>((options, trust) =>
        {
            options.MapInboundClaims = false; options.IncludeErrorDetails = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = trust.Issuer, ValidateAudience = true, ValidAudience = trust.Audience,
                ValidateIssuerSigningKey = true, IssuerSigningKeys = trust.LoadPublicKeys().Select(key => new RsaSecurityKey(key.Parameters) { KeyId = key.KeyId }),
                ValidAlgorithms = [JwtTrustOptions.Algorithm], TryAllIssuerSigningKeys = false, ValidateLifetime = true,
                RequireExpirationTime = true, RequireSignedTokens = true, ClockSkew = TimeSpan.FromSeconds(trust.ClockSkewSeconds), NameClaimType = "name"
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    if (!Guid.TryParse(context.Principal?.FindFirst("sub")?.Value, out var user)
                        || !Guid.TryParse(context.Principal?.FindFirst("sid")?.Value, out var session)
                        || !Guid.TryParse(context.Principal?.FindFirst("sst")?.Value, out var stamp)
                        || !long.TryParse(context.Principal?.FindFirst("exp")?.Value, out var expiry))
                    { context.Fail("Required identity claims are missing."); return; }
                    var access = await context.HttpContext.RequestServices.GetRequiredService<IAuthenticatedSessionReader>()
                        .ReadAsync(new(user, session, stamp, DateTimeOffset.FromUnixTimeSeconds(expiry)), context.HttpContext.RequestAborted,
                            requireVerifiedEmail: false);
                    if (!access.Allowed) context.Fail("The session is no longer active.");
                }
            };
        });
        services.AddAuthorization();
    }
}
