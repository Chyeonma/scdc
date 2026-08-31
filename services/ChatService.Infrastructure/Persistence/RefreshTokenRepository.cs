using ChatService.Data;
using ChatService.Domain.Entities;
using ChatService.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Persistence;

public sealed class RefreshTokenRepository(ChatDbContext dbContext) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.SingleOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            cancellationToken);

    public Task<RefreshToken?> FindByHashForUpdateAsync(
        string tokenHash,
        CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .FromSqlInterpolated($"SELECT * FROM refresh_tokens WHERE token_hash = {tokenHash} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> ListActiveFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken) =>
        await dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

    public void Add(RefreshToken token) => dbContext.RefreshTokens.Add(token);
}
