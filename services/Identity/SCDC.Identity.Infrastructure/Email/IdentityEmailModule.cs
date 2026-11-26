using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SCDC.Modules.Identity.Infrastructure.Email;

internal static class IdentityEmailModule
{
    public static void AddIdentityEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentityEmailOptions>().Bind(configuration.GetSection(IdentityEmailOptions.SectionName))
            .Validate<IHostEnvironment>((options, environment) => options.HasValidOrigin(environment.IsDevelopment()),
                "Email PublicOrigin must be an HTTPS origin; HTTP is allowed in Development.")
            .Validate(options => !options.Enabled || options.HasCredentials(),
                "Enabled Gmail email requires SenderAddress and exactly one AppPassword or AppPasswordFile.")
            .Validate(options => options.PollSeconds is >= 1 and <= 30 && options.BatchSize is >= 1 and <= 100
                && options.SendTimeoutSeconds is >= 1 and <= 20 && options.LeaseSeconds > options.SendTimeoutSeconds + 5
                && options.MaxAttempts is >= 1 and <= 5, "Invalid email worker timing or batch settings.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.KeyRingPath), "Email KeyRingPath is required.")
            .Validate<IHostEnvironment>((options, environment) => environment.IsDevelopment()
                || File.Exists(options.KeyCertificatePath), "Protect the email key ring with KeyCertificatePath outside Development.")
            .ValidateOnStart();
        // Email envelopes and cursor keys have separate rings, application names and purposes.
        services.AddKeyedSingleton<IDataProtectionProvider>("Identity.Email", (provider, _) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<IdentityEmailOptions>>().Value;
            var environment = provider.GetRequiredService<IHostEnvironment>();
            var directory = Directory.CreateDirectory(Path.GetFullPath(options.KeyRingPath, environment.ContentRootPath));
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(directory.FullName, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            return DataProtectionProvider.Create(directory, builder =>
            {
                builder.SetApplicationName("SCDC.Identity.Email");
                if (!string.IsNullOrWhiteSpace(options.KeyCertificatePath))
                    builder.ProtectKeysWithCertificate(X509CertificateLoader.LoadPkcs12FromFile(
                        options.KeyCertificatePath, options.ReadCertificatePassword()));
            });
        });
        services.AddScoped<AccountEmailQueue>();
        services.AddScoped<IdentityEmailProcessor>();
        services.AddSingleton<IAccountEmailSender, GmailSmtpEmailSender>();
        services.AddHostedService<IdentityEmailWorker>();
    }
}
