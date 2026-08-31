using ChatService.Data;
using ChatService.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace ChatService.Infrastructure.Persistence;

public sealed class EfUnitOfWork(ChatDbContext dbContext) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateKeyException("A unique database constraint was violated.", exception);
        }
    }

    public async Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new EfAppTransaction(await dbContext.Database.BeginTransactionAsync(cancellationToken));
}

internal sealed class EfAppTransaction(IDbContextTransaction transaction) : IAppTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);
    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
