namespace SCDC.Modules.Identity.Application;

public sealed record RequestContext(
    string? IpAddress,
    string? UserAgent,
    string? DeviceName = null);

public sealed record RegisterUserCommand(
    string Username,
    string DisplayName,
    string Email,
    string Password,
    RequestContext Context);

public sealed record RegistrationResponse(
    Guid UserId,
    string Username,
    string Email,
    bool VerificationRequired,
    string? DevelopmentVerificationToken);

public sealed record VerifyEmailCommand(string Token, RequestContext Context);

public sealed record LoginCommand(
    string Login,
    string Password,
    RequestContext Context);

public sealed record RefreshSessionCommand(
    string RefreshToken,
    RequestContext Context);

public sealed record LogoutCommand(string RefreshToken, RequestContext Context);

public sealed record ForgotPasswordCommand(string Email, RequestContext Context);

public sealed record PasswordResetRequestedResponse(
    bool Accepted,
    string? DevelopmentResetToken);

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword,
    RequestContext Context);

public sealed record ChangePasswordCommand(
    Guid UserId,
    Guid SessionId,
    string CurrentPassword,
    string NewPassword,
    RequestContext Context);

public sealed record UpdateProfileCommand(
    Guid UserId,
    string DisplayName,
    string? Bio,
    string Locale,
    string Timezone);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserAccountResponse User);

public sealed record UserAccountResponse(
    Guid Id,
    string Username,
    string DisplayName,
    string Email,
    bool EmailVerified,
    string Status,
    string? Bio,
    string? AvatarObjectKey,
    string Locale,
    string Timezone,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version);

public sealed record SessionResponse(
    Guid Id,
    string? DeviceName,
    string? UserAgent,
    string? LastSeenIp,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset ExpiresAt,
    bool IsCurrent);
