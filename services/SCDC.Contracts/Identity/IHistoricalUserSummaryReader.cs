namespace SCDC.Contracts.Identity;

/// <summary>Public labels for already authorized history. Missing accounts return unavailable placeholders;
/// these summaries never grant permission to read, write or deliver a message.</summary>
public interface IHistoricalUserSummaryReader
{
    Task<IReadOnlyDictionary<Guid, UserSummary>> ReadAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
