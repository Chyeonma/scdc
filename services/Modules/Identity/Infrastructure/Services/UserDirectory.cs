using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Identity;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Modules.Identity.Infrastructure.Services;

internal sealed class UserDirectory(IdentityDbContext dbContext) : IUserDirectory
{
    public async Task<UserSummary?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken) => await Query()
        .Where(user => user.Id == userId)
        .Select(user => new UserSummary(
            user.Id,
            user.Username,
            user.Profile!.DisplayName))
        .SingleOrDefaultAsync(cancellationToken);

    public async Task<UserSummary?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();
        return await Query()
            .Where(user => user.NormalizedUsername == normalizedUsername)
            .Select(user => new UserSummary(
                user.Id,
                user.Username,
                user.Profile!.DisplayName))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, UserSummary>> FindByIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, UserSummary>();
        }

        var users = await Query()
            .Where(user => userIds.Contains(user.Id))
            .Select(user => new UserSummary(
                user.Id,
                user.Username,
                user.Profile!.DisplayName))
            .ToListAsync(cancellationToken);
        return users.ToDictionary(user => user.Id);
    }

    private IQueryable<User> Query() => dbContext.Users
        .AsNoTracking()
        .Where(user => user.Status == UserStatus.Active && user.DeletedAt == null);
}
