using System.Data;
using System.Data.Common;
using Npgsql;
using SCDC.Contracts.Persistence;

namespace SCDC.BuildingBlocks.Infrastructure;

public sealed class SharedDatabaseSession(string connectionString) : ISharedDatabaseSession, IAsyncDisposable
{
    private readonly NpgsqlConnection _connection = new(connectionString);
    private Owner? _owner;
    public DbConnection Connection => _connection;
    public DbTransaction? Transaction => _owner?.Transaction;

    public async Task<ISharedDatabaseTransaction> BeginAsync(CancellationToken cancellationToken)
    {
        if (_owner is not null) throw new InvalidOperationException("A shared transaction is already active.");
        if (_connection.State != ConnectionState.Open) await _connection.OpenAsync(cancellationToken);
        _owner = new Owner(this, await _connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken));
        return _owner;
    }

    public async ValueTask DisposeAsync()
    {
        if (_owner is not null) await _owner.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private sealed class Owner(SharedDatabaseSession session, NpgsqlTransaction transaction) : ISharedDatabaseTransaction
    {
        public NpgsqlTransaction Transaction => transaction;
        private bool _committed;
        private bool _disposed;
        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await transaction.CommitAsync(cancellationToken);
            _committed = true;
        }
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                if (!_committed && session._connection.State == ConnectionState.Open)
                {
                    try
                    {
                        if (transaction.Connection is not null)
                            await transaction.RollbackAsync(CancellationToken.None);
                    }
                    catch (ObjectDisposedException)
                    {
                        // A provider failure can already have disposed/rolled back the transaction.
                    }
                }
            }
            finally
            {
                await transaction.DisposeAsync();
                session._owner = null;
            }
        }
    }
}
