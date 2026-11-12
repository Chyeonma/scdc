namespace SCDC.Sample.Domain.Health;

public sealed record ServiceHealth(bool IsHealthy, DateTimeOffset CheckedAt);
