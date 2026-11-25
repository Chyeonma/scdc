using SCDC.Modules.Community.Infrastructure;

namespace SCDC.Community.Api;

internal sealed class CommunityDatabaseStartupGate(CommunityDatabaseReadiness database, IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        if (!configuration.GetValue("InternalRpc:Enabled", true)
            || !await database.IsReadyAsync(ct))
            throw new InvalidOperationException("Integrated Community requires mTLS and an isolated Community runtime database.");
        foreach (var owner in new[] { "Identity", "Messaging" })
            if (!Uri.TryCreate(configuration[$"InternalRpc:{owner}:Endpoint"], UriKind.Absolute, out var endpoint) || endpoint.Scheme != "https")
                throw new InvalidOperationException("Integrated Community requires HTTPS service endpoints.");
    }
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
