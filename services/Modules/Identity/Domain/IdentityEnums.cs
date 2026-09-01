namespace SCDC.Modules.Identity.Domain;

internal enum UserStatus : short
{
    PendingVerification = 0,
    Active = 1,
    Suspended = 2,
    Disabled = 3,
    Deleted = 4
}

internal enum AccountTokenPurpose : short
{
    VerifyEmail = 1,
    ResetPassword = 2,
    ChangeEmail = 3,
    UnlockAccount = 4
}
