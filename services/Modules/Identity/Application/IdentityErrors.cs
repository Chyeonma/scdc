using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Identity.Application;

internal static class IdentityErrors
{
    public static readonly Error UsernameTaken = Error.Conflict(
        "Identity.UsernameAlreadyExists",
        "The username is already in use.");

    public static readonly Error EmailTaken = Error.Conflict(
        "Identity.EmailAlreadyExists",
        "The email address is already in use.");

    public static readonly Error RegistrationConflict = Error.Conflict(
        "Identity.RegistrationConflict",
        "The account conflicts with existing data.");

    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Identity.InvalidCredentials",
        "The supplied credentials are invalid.");

    public static readonly Error EmailNotVerified = Error.Forbidden(
        "Identity.EmailNotVerified",
        "The primary email address has not been verified.");

    public static readonly Error AccountUnavailable = Error.Forbidden(
        "Identity.AccountUnavailable",
        "The account is not available for sign-in.");

    public static Error AccountLocked(DateTimeOffset lockedUntil) => Error.TooManyRequests(
        "Identity.AccountLocked",
        $"The account is temporarily locked until {lockedUntil:O}.");

    public static readonly Error InvalidAccountToken = Error.Validation(
        "Identity.InvalidOrExpiredToken",
        "The account token is invalid or has expired.");

    public static readonly Error InvalidRefreshToken = Error.Unauthorized(
        "Identity.InvalidRefreshToken",
        "The refresh token is invalid or has expired.");

    public static readonly Error RefreshTokenReuse = Error.Unauthorized(
        "Identity.RefreshTokenReuseDetected",
        "Refresh token reuse was detected and the session was revoked.");

    public static readonly Error UserNotFound = Error.NotFound(
        "Identity.UserNotFound",
        "The user was not found.");

    public static readonly Error SessionNotFound = Error.NotFound(
        "Identity.SessionNotFound",
        "The session was not found.");

    public static readonly ValidationError CurrentPasswordInvalid = new(
        "Identity.CurrentPasswordInvalid",
        "The current password is invalid.",
        new Dictionary<string, string[]>
        {
            ["currentPassword"] = ["The current password is invalid."]
        });

    public static readonly ValidationError PasswordUnchanged = new(
        "Identity.PasswordUnchanged",
        "The new password must be different from the current password.",
        new Dictionary<string, string[]>
        {
            ["newPassword"] = ["The new password must be different from the current password."]
        });
}
