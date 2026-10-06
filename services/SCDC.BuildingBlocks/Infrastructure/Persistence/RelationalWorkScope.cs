using System.Data;
using Npgsql;

namespace SCDC.BuildingBlocks.Infrastructure.Persistence;

public sealed class RelationalWorkScopeFactory(NpgsqlDataSource dataSource)
{
    public async Task<RelationalWorkScope> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        try
        {
            var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            var scope = new RelationalWorkScope(connection, transaction);
            await using var command = scope.CreateCommand("SET LOCAL lock_timeout = '2s'");
            await command.ExecuteNonQueryAsync(cancellationToken);
            return scope;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}

public sealed class RelationalWorkScope(NpgsqlConnection connection, NpgsqlTransaction transaction)
    : IAsyncDisposable
{
    public NpgsqlConnection Connection { get; } = connection;
    public NpgsqlTransaction Transaction { get; } = transaction;

    public NpgsqlCommand CreateCommand(string sql) => new(sql, Connection, Transaction) { CommandTimeout = 5 };

    public Task CommitAsync(CancellationToken cancellationToken) => Transaction.CommitAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Transaction.DisposeAsync();
        }
        finally
        {
            await Connection.DisposeAsync();
        }
    }
}
