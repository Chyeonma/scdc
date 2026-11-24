using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SCDC.BuildingBlocks.Infrastructure.Persistence;

namespace SCDC.Modules.Community.Infrastructure;

internal sealed class ChannelProvisionWorker(RelationalWorkScopeFactory scopes, ChannelProvisionCoordinator coordinator,
    ILogger<ChannelProvisionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextWarning = DateTimeOffset.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var operations = new List<Guid>();
                await using (var local = await scopes.OpenAsync(stoppingToken))
                {
                    await using var query = local.CreateCommand("SELECT operation_id FROM community.channel_provisions WHERE delivery_pending AND next_attempt_at<=clock_timestamp() AND(lease_until IS NULL OR lease_until<clock_timestamp()) ORDER BY next_attempt_at,created_at LIMIT 4");
                    await using var rows = await query.ExecuteReaderAsync(stoppingToken);
                    while (await rows.ReadAsync(stoppingToken)) operations.Add(rows.GetGuid(0));
                }
                await Task.WhenAll(operations.Select(id => coordinator.ProcessAsync(id, stoppingToken)));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error)
            {
                if (DateTimeOffset.UtcNow >= nextWarning)
                { logger.LogWarning("Channel worker deferred ({FailureType})", error.GetType().Name); nextWarning = DateTimeOffset.UtcNow.AddSeconds(10); }
            }
            await Task.Delay(100, stoppingToken);
        }
    }
}
