using Microsoft.Extensions.DependencyInjection;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Messaging.Infrastructure.Access;

// Labels only. A fresh Identity scope cannot enlist in a caller's Messaging transaction.
internal sealed class MessagingProfiles(IServiceScopeFactory scopes)
{
    public async Task<IReadOnlyDictionary<Guid, UserSummary>> ReadAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var wanted = ids.Distinct().ToArray();
        if (wanted.Length == 0) return new Dictionary<Guid, UserSummary>();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct); deadline.CancelAfter(TimeSpan.FromSeconds(2));
        await using var scope = scopes.CreateAsyncScope();
        try
        {
            var people = await scope.ServiceProvider.GetRequiredService<IHistoricalUserSummaryReader>().ReadAsync(wanted, deadline.Token).WaitAsync(deadline.Token);
            if (wanted.Any(id => !people.ContainsKey(id))) throw Unavailable();
            return people;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw Unavailable(); }
        catch (Exception failure) when (DatabaseAvailability.IsUnavailable(failure)) { throw Unavailable(); }
    }
    private static MessagingAccessFailure Unavailable() => new(Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", "Public user summaries are temporarily unavailable."));
}
