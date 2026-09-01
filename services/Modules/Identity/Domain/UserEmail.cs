namespace SCDC.Modules.Identity.Domain;

internal sealed class UserEmail
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Email { get; set; }
    public string NormalizedEmail { get; private set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public User User { get; set; } = null!;
}
