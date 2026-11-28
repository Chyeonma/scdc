using System.Data;
using System.Data.Common;
using Npgsql;


namespace SCDC.Modules.Messaging.Infrastructure.Persistence;

internal sealed class MessagingSession(string connectionString) : IAsyncDisposable
{
    private readonly NpgsqlConnection _connection = new(connectionString);
    private Owner? _owner;
    public DbConnection Connection => _connection;
    public DbTransaction? Transaction => _owner?.Transaction;

    public async Task<MessagingSession.Owner> BeginAsync(CancellationToken cancellationToken)
    {
        if (_owner is not null) throw new InvalidOperationException("A Messaging transaction is already active.");
        if (_connection.State != ConnectionState.Open) await _connection.OpenAsync(cancellationToken);
        _owner = new Owner(this, await _connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken));
        return _owner;
    }

    public async ValueTask DisposeAsync()
    {
        if (_owner is not null) await _owner.DisposeAsync();
        await _connection.DisposeAsync();
    }

    internal sealed class Owner(MessagingSession session, NpgsqlTransaction transaction) : IAsyncDisposable
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
