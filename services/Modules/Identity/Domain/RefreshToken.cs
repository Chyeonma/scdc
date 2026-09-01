namespace SCDC.Modules.Identity.Domain;

internal sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid? ParentTokenId { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
    public AuthSession Session { get; set; } = null!;
}
