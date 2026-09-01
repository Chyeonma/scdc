namespace SCDC.Modules.Identity.Infrastructure;

internal sealed class IdentityOptions
{
    public const string SectionName = "Modules:Identity";

    public string Issuer { get; init; } = "SCDC";
    public string Audience { get; init; } = "SCDC.WebClient";
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int SessionDays { get; init; } = 30;
    public int EmailVerificationTokenMinutes { get; init; } = 30;
    public int PasswordResetTokenMinutes { get; init; } = 30;
    public int MaxFailedLoginAttempts { get; init; } = 5;
    public int LockoutMinutes { get; init; } = 15;
    public bool ExposeDevelopmentTokens { get; init; }
}
