using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Identity.Application;

internal static class IdentityErrors
{
    public static Error UsernameTaken => Error.Conflict(
        "Identity.UsernameAlreadyExists",
        "The username is already in use.");

    public static Error EmailTaken => Error.Conflict(
        "Identity.EmailAlreadyExists",
        "The email address is already in use.");

    public static Error InvalidCredentials => Error.Unauthorized(
        "Identity.InvalidCredentials",
        "The supplied credentials are invalid.");

    public static Error EmailNotVerified => Error.Forbidden(
        "Identity.EmailNotVerified",
        "The primary email address has not been verified.");

    public static Error AccountUnavailable => Error.Forbidden(
        "Identity.AccountUnavailable",
        "The account is not available for sign-in.");

    public static Error AccountLocked(DateTimeOffset lockedUntil) => Error.TooManyRequests(
        "Identity.AccountLocked",
        $"The account is temporarily locked until {lockedUntil:O}.");

    public static Error InvalidAccountToken => Error.Validation(
        "Identity.InvalidOrExpiredToken",
        "The account token is invalid or has expired.");

    public static Error InvalidRefreshToken => Error.Unauthorized(
        "Identity.InvalidRefreshToken",
        "The refresh token is invalid or has expired.");

    public static Error RefreshTokenReuse => Error.Unauthorized(
        "Identity.RefreshTokenReuseDetected",
        "Refresh token reuse was detected and the session was revoked.");

    public static Error UserNotFound => Error.NotFound(
        "Identity.UserNotFound",
        "The user was not found.");

    public static Error SessionNotFound => Error.NotFound(
        "Identity.SessionNotFound",
        "The session was not found.");
}
