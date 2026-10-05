using System.Text;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using SCDC.BuildingBlocks.Application;
using SCDC.Contracts.Identity;
using SCDC.Modules.Identity.Application;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure;
using SCDC.Modules.Identity.Infrastructure.Email;
using SCDC.Modules.Identity.Infrastructure.Persistence;
using SCDC.Modules.Identity.Infrastructure.Security;
using SCDC.Modules.Identity.Infrastructure.Services;
using ModuleIdentityOptions = SCDC.Modules.Identity.Infrastructure.IdentityOptions;

namespace SCDC.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Database must be configured.");
        }

        var identitySection = configuration.GetSection(ModuleIdentityOptions.SectionName);
        var identityOptions = identitySection.Get<ModuleIdentityOptions>()
            ?? throw new InvalidOperationException("Modules:Identity must be configured.");

        services.AddOptions<ModuleIdentityOptions>()
            .Bind(identitySection)
            .Validate(options => options.SigningKey.Length >= 32, "Identity signing key must contain at least 32 characters.")
            .Validate(options => options.AccessTokenMinutes > 0, "Access token lifetime must be positive.")
            .Validate(options => options.SessionDays > 0, "Session lifetime must be positive.")
            .Validate(options => options.MaxFailedLoginAttempts > 0, "Lockout threshold must be positive.")
            .Validate(options => options.LockoutMinutes > 0 && options.EmailVerificationTokenMinutes > 0
                && options.PasswordResetTokenMinutes > 0, "Lockout and account token lifetimes must be positive.")
            .Validate(options => options.AccountTokenCooldownSeconds >= 60, "Account token cooldown must be at least 60 seconds.")
            .ValidateOnStart();

        var emailSection = configuration.GetSection(IdentityEmailOptions.SectionName);
        var emailOptions = emailSection.Get<IdentityEmailOptions>() ?? new IdentityEmailOptions();
        services.AddOptions<IdentityEmailOptions>().Bind(emailSection)
            .Validate(options => options.HasValidOrigin(environment.IsDevelopment()), "Email PublicOrigin must be a trusted HTTPS origin (HTTP allowed in Development).")
            .Validate(options => !options.Enabled || options.HasCredentials(), "Gmail SMTP requires SenderAddress and AppPassword when enabled.")
            .Validate(options => options.PollSeconds is >= 1 and <= 30 && options.BatchSize is >= 1 and <= 100
                && options.SendTimeoutSeconds is >= 1 and <= 20 && options.LeaseSeconds > options.SendTimeoutSeconds + 5
                && options.MaxAttempts is >= 1 and <= 5, "Email worker timing or batch settings are invalid.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.KeyRingPath), "Email KeyRingPath must be configured.")
            .Validate(options => environment.IsDevelopment() || !string.IsNullOrWhiteSpace(options.KeyCertificatePath),
                "Outside Development, protect the email key ring with KeyCertificatePath.")
            .ValidateOnStart();
        var protection = services.AddDataProtection().SetApplicationName("SCDC.Identity")
            .PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(emailOptions.KeyRingPath, environment.ContentRootPath)));
        if (!string.IsNullOrWhiteSpace(emailOptions.KeyCertificatePath))
        {
            protection.ProtectKeysWithCertificate(X509CertificateLoader.LoadPkcs12FromFile(
                emailOptions.KeyCertificatePath, emailOptions.KeyCertificatePassword));
        }

        services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<AccountEmailQueue>();
        services.AddScoped<IdentityEmailProcessor>();
        services.AddScoped<IdentityMaintenance>();
        services.AddSingleton<IAccountEmailSender, GmailSmtpEmailSender>();
        services.AddHostedService<IdentityEmailWorker>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IUserDirectory, UserDirectory>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = identityOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = identityOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(identityOptions.SigningKey)),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name"
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ValidateSessionAsync
                };
            });
        services.AddAuthorization();
        services.AddSingleton<IModuleDescriptor, IdentityModuleDescriptor>();
        return services;
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

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<IdentityDbContext>();
        var timeProvider = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
        var now = timeProvider.GetUtcNow();
        var isActive = await dbContext.AuthSessions
            .AsNoTracking()
            .AnyAsync(session => session.Id == sessionId
                && session.UserId == userId
                && session.RevokedAt == null
                && session.ExpiresAt > now
                && session.User.Status == UserStatus.Active
                && session.User.SecurityState != null
                && session.User.SecurityState.SecurityStamp == securityStamp,
                context.HttpContext.RequestAborted);

        if (!isActive)
        {
            context.Fail("The session is no longer active.");
        }
    }

    private sealed class IdentityModuleDescriptor : IModuleDescriptor
    {
        public string Name => "Identity";
        public string DatabaseSchema => "identity";
        public ModuleStage Stage => ModuleStage.Active;
    }
}
