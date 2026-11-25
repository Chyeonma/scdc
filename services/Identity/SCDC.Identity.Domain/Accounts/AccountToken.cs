using System.Net;

namespace SCDC.Modules.Identity.Domain;

internal sealed class AccountToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public AccountTokenPurpose Purpose { get; set; }
    public required string TokenHash { get; set; }
    public string? TargetValue { get; set; }
    public IPAddress? CreatedByIp { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public User User { get; set; } = null!;
}
