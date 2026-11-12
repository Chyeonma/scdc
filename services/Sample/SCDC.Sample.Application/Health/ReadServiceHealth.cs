namespace SCDC.Sample.Application.Health;

public sealed record HealthResponse(string Service, string Status, DateTimeOffset CheckedAt);

public sealed class ReadServiceHealth(IHealthProbe probe)
{
    public HealthResponse Execute()
    {
        var health = probe.Check();
        return new("sample-service", health.IsHealthy ? "healthy" : "unhealthy", health.CheckedAt);
    }
}
