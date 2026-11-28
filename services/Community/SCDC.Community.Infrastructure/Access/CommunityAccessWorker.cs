using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SCDC.Modules.Community.Infrastructure.Persistence;

namespace SCDC.Modules.Community.Infrastructure.Access;

internal sealed class CommunityAccessWorker(RelationalWorkScopeFactory scopes,CommunityPolicyCoordinator coordinator,
    ILogger<CommunityAccessWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextWarning=DateTimeOffset.MinValue;
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pending=new List<Guid>();
                await using(var local=await scopes.OpenAsync(stoppingToken))
                {
                    await using var query=local.CreateCommand("SELECT operation_id FROM community.access_changes WHERE phase=0 AND next_attempt_at<=clock_timestamp() AND (lease_until IS NULL OR lease_until<clock_timestamp()) ORDER BY created_at LIMIT 4");
                    await using var rows=await query.ExecuteReaderAsync(stoppingToken);while(await rows.ReadAsync(stoppingToken))pending.Add(rows.GetGuid(0));
                }
                await Task.WhenAll(pending.Select(id=>coordinator.ProcessAsync(id,stoppingToken)));
            }
            catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception error)
            {
                if(DateTimeOffset.UtcNow>=nextWarning)
                {logger.LogWarning("Community access work deferred {ErrorCode} {FailureType}","ACCESS_CHECK_UNAVAILABLE",error.GetType().Name);nextWarning=DateTimeOffset.UtcNow.AddSeconds(10);}
            }
            await Task.Delay(250,stoppingToken);
        }
    }
}
