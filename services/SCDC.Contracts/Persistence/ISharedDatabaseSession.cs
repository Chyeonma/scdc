using System.Data.Common;

namespace SCDC.Contracts.Persistence;

/// <summary>One scoped connection; only the transaction owner commits or rolls back.</summary>
public interface ISharedDatabaseSession
{
    DbConnection Connection { get; }
    DbTransaction? Transaction { get; }
    Task<ISharedDatabaseTransaction> BeginAsync(CancellationToken cancellationToken);
}

public interface ISharedDatabaseTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
