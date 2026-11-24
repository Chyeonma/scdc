using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using SCDC.BuildingBlocks.Application;
using SCDC.Contracts.Identity;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.Modules.Identity.Application;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure;
using SCDC.Modules.Identity.Infrastructure.Persistence;
using SCDC.Modules.Identity.Infrastructure.Security;
using SCDC.Modules.Identity.Infrastructure.Services;
using ModuleIdentityOptions = SCDC.Modules.Identity.Infrastructure.IdentityOptions;
using SCDC.Shared.Core.Authentication;
using SCDC.BuildingBlocks.Infrastructure.Authentication;

namespace SCDC.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Identity");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Identity must be configured.");
        }

        var identitySection = configuration.GetSection(ModuleIdentityOptions.SectionName);
        services.AddOptions<JwtTrustOptions>()
            .Bind(configuration.GetSection(JwtTrustOptions.SectionName))
            .Validate(options => { _ = options.LoadPublicKeys(); return true; }, "Invalid JWT trust.")
            .ValidateOnStart();
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<JwtTrustOptions>>().Value);
        services.AddSingleton(provider => new IdentitySigningKey(
            provider.GetRequiredService<IOptions<ModuleIdentityOptions>>().Value,
            provider.GetRequiredService<JwtTrustOptions>()));

        services.AddOptions<ModuleIdentityOptions>()
            .Bind(identitySection)
            .Validate(options => File.Exists(options.PrivateKeyPath), "Identity private key file must exist.")
            .Validate<JwtTrustOptions>((options, trust) => { _ = new IdentitySigningKey(options, trust); return true; }, "Invalid Identity signing key.")
            .Validate(options => options.AccessTokenMinutes > 0, "Access token lifetime must be positive.")
            .Validate(options => options.SessionDays > 0, "Session lifetime must be positive.")
            .Validate(options => options.MaxFailedLoginAttempts > 0, "Lockout threshold must be positive.")
            .ValidateOnStart();

        services.AddDbContext<IdentityDbContext>((provider, options) =>
            options.UseNpgsql(ModuleDatabaseConnection.Get(provider.GetRequiredService<IConfiguration>(), "Identity")));
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<IUserSearchDirectory, UserSearchDirectory>();
        services.AddSingleton(provider => new RedisSessionConnection(provider.GetRequiredService<IConfiguration>(), "Reader"));
        services.AddSingleton<RedisSessionReader>();
        services.AddSingleton<IAuthenticatedSessionReader>(provider => provider.GetRequiredService<RedisSessionReader>());
        services.AddSingleton<ISessionStoreReadiness>(provider => provider.GetRequiredService<RedisSessionReader>());
        services.AddSingleton<SessionPublisher>();
        services.AddSingleton<IdentityRevocations>();
        services.AddSingleton<IRevocationStatusReader>(provider => provider.GetRequiredService<IdentityRevocations>());
        services.AddHostedService<IdentityAccessWorker>();
        foreach (var owner in new[] { AccessOwner.Community, AccessOwner.Messaging })
            services.AddKeyedScoped<IIdentityPermitIssuer>(owner, (provider, _) => new IdentityPermitIssuer(
                provider.GetRequiredService<IConfiguration>(), owner, provider.GetRequiredService<TimeProvider>()));
        services.AddScoped<IHistoricalUserSummaryReader, HistoricalUserSummaryReader>();
        services.AddScoped<UserSearchKeyInitializer>();
        var searchKeyRing = configuration["Modules:Identity:UserSearch:CursorKeyRingPath"]
            ?? Path.Combine(AppContext.BaseDirectory, ".dm-keys", "user-search");
        services.AddKeyedSingleton<IDataProtectionProvider>("Messaging.Cursors", (_, _) =>
            DataProtectionProvider.Create(new DirectoryInfo(searchKeyRing), builder =>
                builder.SetApplicationName(configuration["Modules:Identity:UserSearch:ApplicationName"] ?? "SCDC.UserSearch")));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtTrustOptions>((options, trust) =>
            {
                options.MapInboundClaims = false;
                options.IncludeErrorDetails = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = trust.Issuer,
                    ValidateAudience = true,
                    ValidAudience = trust.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = trust.LoadPublicKeys().Select(key => new RsaSecurityKey(key.Parameters) { KeyId = key.KeyId }),
                    ValidAlgorithms = [JwtTrustOptions.Algorithm],
                    TryAllIssuerSigningKeys = false,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromSeconds(trust.ClockSkewSeconds),
                    NameClaimType = "name"
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Browsers cannot set an Authorization header on the WebSocket handshake.
                        // Never accept query credentials on REST or another Hub route.
                        if (context.Request.Path == "/hubs/chat" && context.Request.Method == "GET"
                            && context.Request.Query.TryGetValue("access_token", out var token))
                            context.Token = token.ToString();
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = ValidateSessionAsync
                };
            });
        services.AddAuthorization();
        services.AddSingleton<IModuleDescriptor, IdentityModuleDescriptor>();
        return services;
    }

    public static void InitializeSigningKeys(IConfiguration configuration)
    {
        var options = configuration.GetSection(ModuleIdentityOptions.SectionName).Get<ModuleIdentityOptions>() ?? new();
        var trust = configuration.GetSection(JwtTrustOptions.SectionName).Get<JwtTrustOptions>() ?? new();
        IdentitySigningKey.Initialize(options, trust);
    }

    private static async Task ValidateSessionAsync(TokenValidatedContext context)
    {
        var subject = context.Principal?.FindFirst("sub")?.Value;
        var sessionClaim = context.Principal?.FindFirst("sid")?.Value;
        var stampClaim = context.Principal?.FindFirst("sst")?.Value;
        if (!Guid.TryParse(subject, out var userId)
            || !Guid.TryParse(sessionClaim, out var sessionId)
            || !Guid.TryParse(stampClaim, out var securityStamp))
        {
            context.Fail("Required identity claims are missing.");
            return;
        }

        // REST owners distinguish an authenticated account from permission denial.
        // Hub negotiation must reject an ineligible account before opening a connection.
        var isChatHandshake = context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat");
        var expiry = context.Principal?.FindFirst("exp")?.Value;
        if (!long.TryParse(expiry, out var seconds)) { context.Fail("Expiration is missing."); return; }
        var access = await context.HttpContext.RequestServices.GetRequiredService<IAuthenticatedSessionReader>()
            .ReadAsync(new(userId, sessionId, securityStamp, DateTimeOffset.FromUnixTimeSeconds(seconds)),
                context.HttpContext.RequestAborted, isChatHandshake);

        if (!access.Allowed)
        {
            context.Fail("The session is no longer active.");
        }
    }

    public static async Task InitializeUserSearchKeysAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<UserSearchKeyInitializer>().InitializeAsync(cancellationToken);
    }

    private sealed class IdentityModuleDescriptor : IModuleDescriptor
    {
        public string Name => "Identity";
        public string DatabaseSchema => "identity";
        public ModuleStage Stage => ModuleStage.Active;
    }
}
