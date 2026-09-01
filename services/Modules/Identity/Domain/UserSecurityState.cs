namespace SCDC.Modules.Identity.Domain;

internal sealed class UserSecurityState
{
    public Guid UserId { get; set; }
    public Guid SecurityStamp { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LastFailedLoginAt { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset? LastSuccessfulLoginAt { get; set; }
    public bool MfaEnabled { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public User User { get; set; } = null!;
}
