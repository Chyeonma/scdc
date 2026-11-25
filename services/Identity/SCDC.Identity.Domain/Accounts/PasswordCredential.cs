namespace SCDC.Modules.Identity.Domain;

internal sealed class PasswordCredential
{
    public Guid UserId { get; set; }
    public required string PasswordHash { get; set; }
    public required string HashAlgorithm { get; set; }
    public int PasswordVersion { get; set; }
    public DateTimeOffset PasswordChangedAt { get; set; }
    public bool RequiresChange { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public User User { get; set; } = null!;
}
