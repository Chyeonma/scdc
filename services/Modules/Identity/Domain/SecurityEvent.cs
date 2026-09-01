using System.Net;

namespace SCDC.Modules.Identity.Domain;

internal sealed class SecurityEvent
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public required string EventType { get; set; }
    public IPAddress? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public required string Metadata { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
