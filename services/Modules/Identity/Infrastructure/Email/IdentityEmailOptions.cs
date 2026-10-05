using System.Net.Mail;

namespace SCDC.Modules.Identity.Infrastructure.Email;

internal sealed class IdentityEmailOptions
{
    public const string SectionName = "Modules:Identity:Email";
    public bool Enabled { get; init; }
    public string PublicOrigin { get; init; } = "http://localhost:3000";
    public string KeyRingPath { get; init; } = "../../artifacts/identity-keys";
    public string KeyCertificatePath { get; init; } = "";
    public string KeyCertificatePassword { get; init; } = "";
    public string SenderAddress { get; init; } = "";
    public string AppPassword { get; init; } = "";
    public int PollSeconds { get; init; } = 2;
    public int BatchSize { get; init; } = 10;
    public int LeaseSeconds { get; init; } = 30;
    public int SendTimeoutSeconds { get; init; } = 10;
    public int MaxAttempts { get; init; } = 5;

    public bool HasValidOrigin(bool development) =>
        Uri.TryCreate(PublicOrigin, UriKind.Absolute, out var origin)
        && (origin.Scheme == Uri.UriSchemeHttps || development && origin.Scheme == Uri.UriSchemeHttp)
        && string.IsNullOrEmpty(origin.UserInfo)
        && string.IsNullOrEmpty(origin.Query)
        && string.IsNullOrEmpty(origin.Fragment)
        && origin.AbsolutePath == "/";

    public bool HasCredentials() => MailAddress.TryCreate(SenderAddress, out var address)
        && address.Address == SenderAddress
        && !string.IsNullOrWhiteSpace(AppPassword);
}
