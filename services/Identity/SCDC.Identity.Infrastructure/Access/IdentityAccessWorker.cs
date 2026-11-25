using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace SCDC.Modules.Identity.Infrastructure.Services;

internal sealed class IdentityAccessWorker(SessionPublisher publisher, IdentityRevocations revocations,
    IConfiguration configuration, ILogger<IdentityAccessWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextWarning = DateTimeOffset.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await publisher.SynchronizeAsync(stoppingToken);
                await using var connection = new NpgsqlConnection(ModuleDatabaseConnection.Get(configuration, "Identity"));
                await connection.OpenAsync(stoppingToken);
                var pending = new List<Guid>();
                await using (var query = new NpgsqlCommand("""
                    SELECT operation_id FROM identity.access_changes WHERE phase=0
                    AND next_attempt_at<=clock_timestamp() AND (lease_until IS NULL OR lease_until<clock_timestamp())
                    ORDER BY created_at LIMIT 4
                    """, connection))
                await using (var rows = await query.ExecuteReaderAsync(stoppingToken))
                    while (await rows.ReadAsync(stoppingToken)) pending.Add(rows.GetGuid(0));
                await Task.WhenAll(pending.Select(id => revocations.ProcessAsync(id, stoppingToken)));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error)
            {
                if (DateTimeOffset.UtcNow >= nextWarning)
                {
                    logger.LogWarning("Identity access work deferred {ErrorCode} {FailureType}", "ACCESS_CHECK_UNAVAILABLE", error.GetType().Name);
                    nextWarning = DateTimeOffset.UtcNow.AddSeconds(10);
                }
            }
            await Task.Delay(500, stoppingToken);
        }
    }
}
