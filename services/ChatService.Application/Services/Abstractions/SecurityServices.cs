using System.Net;
using ChatService.Domain.Entities;

namespace ChatService.Services.Abstractions;

public interface IPasswordService
{
    string Hash(User user, string password);
    PasswordCheckResult Verify(User user, string passwordHash, string password);
    void PerformDummyHash(string password);
}

public enum PasswordCheckResult
{
    Failed,
    Success,
    SuccessRehashNeeded
}

public interface ITokenService
{
    AccessTokenData CreateAccessToken(User user);

    RefreshTokenData CreateRefreshToken(
        Guid userId,
        IPAddress? createdByIp,
        Guid? familyId = null,
        Guid? parentId = null);

    string HashRefreshToken(string rawToken);
}

public sealed record AccessTokenData(string Token, DateTimeOffset ExpiresAt);
public sealed record RefreshTokenData(string RawToken, RefreshToken Entity);
