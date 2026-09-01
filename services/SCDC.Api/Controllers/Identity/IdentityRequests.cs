using System.ComponentModel.DataAnnotations;

namespace SCDC.Api.Controllers.Identity;

public sealed record RegisterRequest(
    [param: Required]
    [param: RegularExpression("^[A-Za-z0-9_.]{3,32}$")]
    string Username,
    [param: Required, StringLength(64, MinimumLength = 1)]
    string DisplayName,
    [param: Required, EmailAddress, StringLength(254)]
    string Email,
    [param: Required, StringLength(128, MinimumLength = 8)]
    string Password);

public sealed record VerifyEmailRequest(
    [param: Required, StringLength(512, MinimumLength = 20)]
    string Token);

public sealed record LoginRequest(
    [param: Required, StringLength(254, MinimumLength = 3)]
    string Login,
    [param: Required, StringLength(128, MinimumLength = 1)]
    string Password,
    [param: StringLength(100)]
    string? DeviceName);

public sealed record RefreshRequest(
    [param: Required, StringLength(512, MinimumLength = 20)]
    string RefreshToken);

public sealed record LogoutRequest(
    [param: Required, StringLength(512, MinimumLength = 20)]
    string RefreshToken);

public sealed record ForgotPasswordRequest(
    [param: Required, EmailAddress, StringLength(254)]
    string Email);

public sealed record ResetPasswordRequest(
    [param: Required, StringLength(512, MinimumLength = 20)]
    string Token,
    [param: Required, StringLength(128, MinimumLength = 8)]
    string NewPassword);

public sealed record ChangePasswordRequest(
    [param: Required, StringLength(128, MinimumLength = 1)]
    string CurrentPassword,
    [param: Required, StringLength(128, MinimumLength = 8)]
    string NewPassword);

public sealed record UpdateProfileRequest(
    [param: Required, StringLength(64, MinimumLength = 1)]
    string DisplayName,
    [param: StringLength(500)]
    string? Bio,
    [param: Required, StringLength(16, MinimumLength = 1)]
    string Locale,
    [param: Required, StringLength(64, MinimumLength = 1)]
    string Timezone);
