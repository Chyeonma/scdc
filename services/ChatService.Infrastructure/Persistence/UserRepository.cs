using ChatService.Data;
using ChatService.Domain.Entities;
using ChatService.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Persistence;

public sealed class UserRepository(ChatDbContext dbContext) : IUserRepository
{
    public Task<bool> ExistsAsync(
        string normalizedEmail,
        string normalizedUsername,
        CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(
            user => user.NormalizedEmail == normalizedEmail || user.NormalizedUsername == normalizedUsername,
            cancellationToken);

    public Task<User?> FindByLoginAsync(string normalizedLogin, CancellationToken cancellationToken) =>
        dbContext.Users.SingleOrDefaultAsync(
            user => user.NormalizedUsername == normalizedLogin || user.NormalizedEmail == normalizedLogin,
            cancellationToken);

    public Task<User?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken,
        bool tracking = true)
    {
        var query = tracking ? dbContext.Users : dbContext.Users.AsNoTracking();
        return query.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public Task<User?> FindByNormalizedUsernameAsync(
        string normalizedUsername,
        CancellationToken cancellationToken) =>
        dbContext.Users.SingleOrDefaultAsync(
            user => user.NormalizedUsername == normalizedUsername,
            cancellationToken);

    public void Add(User user) => dbContext.Users.Add(user);
}
