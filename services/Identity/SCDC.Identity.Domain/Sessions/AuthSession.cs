using System.Net;

namespace SCDC.Modules.Identity.Domain;

internal sealed class AuthSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? DeviceName { get; set; }
    public string? UserAgent { get; set; }
    public IPAddress? CreatedByIp { get; set; }
    public IPAddress? LastSeenIp { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
    public User User { get; set; } = null!;
    public ICollection<RefreshToken> RefreshTokens { get; } = new List<RefreshToken>();
}
