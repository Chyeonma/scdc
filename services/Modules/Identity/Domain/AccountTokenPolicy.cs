namespace SCDC.Modules.Identity.Domain;

internal sealed class AccountTokenPolicy
{
    public Guid UserId { get; set; }
    public AccountTokenPurpose Purpose { get; set; }
    public DateTimeOffset LastIssuedAt { get; set; }
    public Guid? ActiveTokenId { get; set; }
}
