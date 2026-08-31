using ChatService.Domain.Entities;
using ChatService.Dtos;
using ChatService.Services.Abstractions;

namespace ChatService.Services;

public sealed class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    IPasswordService passwords,
    ITokenService tokens,
    IRequestContext requestContext,
    TimeProvider timeProvider) : IAuthService
{
    private const int LockoutThreshold = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var username = request.Username.Trim();
        var displayName = request.DisplayName.Trim();
        if (displayName.Length == 0)
        {
            return ServiceResult<AuthResponse>.Validation(
                "displayName",
                "Display name cannot contain only whitespace.");
        }

        var normalizedEmail = Normalize(email);
        var normalizedUsername = Normalize(username);
        var exists = await users.ExistsAsync(normalizedEmail, normalizedUsername, cancellationToken);
        if (exists)
        {
            return ServiceResult<AuthResponse>.Conflict("Email or username already exists.");
        }

        var now = timeProvider.GetUtcNow();
        var user = new User
        {
            Email = email,
            NormalizedEmail = normalizedEmail,
            Username = username,
            NormalizedUsername = normalizedUsername,
            DisplayName = displayName,
            CreatedAt = now,
            UpdatedAt = now
        };
        user.PasswordHash = passwords.Hash(user, request.Password);
        var refreshToken = tokens.CreateRefreshToken(user.Id, requestContext.ClientIp);

        users.Add(user);
        refreshTokens.Add(refreshToken.Entity);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateKeyException)
        {
            return ServiceResult<AuthResponse>.Conflict("Email or username already exists.");
        }

        var accessToken = tokens.CreateAccessToken(user);
        return ServiceResult<AuthResponse>.Created(CreateAuthResponse(user, accessToken, refreshToken));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedLogin = Normalize(request.Login);
        var user = await users.FindByLoginAsync(normalizedLogin, cancellationToken);
        var now = timeProvider.GetUtcNow();

        if (user is null)
        {
            passwords.PerformDummyHash(request.Password);
            return LoginFailed();
        }

        if (user.LockoutEnd is not null && user.LockoutEnd > now)
        {
            return ServiceResult<AuthResponse>.TooManyRequests(
                "Try again after the temporary lockout expires.");
        }

        var verification = passwords.Verify(user, user.PasswordHash, request.Password);
        if (verification == PasswordCheckResult.Failed)
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= LockoutThreshold)
            {
                user.LockoutEnd = now.Add(LockoutDuration);
                user.AccessFailedCount = 0;
            }

            user.UpdatedAt = now;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return LoginFailed();
        }

        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        user.UpdatedAt = now;
        if (verification == PasswordCheckResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwords.Hash(user, request.Password);
        }

        var refreshToken = tokens.CreateRefreshToken(user.Id, requestContext.ClientIp);
        refreshTokens.Add(refreshToken.Entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = tokens.CreateAccessToken(user);
        return ServiceResult<AuthResponse>.Ok(CreateAuthResponse(user, accessToken, refreshToken));
    }

    public async Task<ServiceResult<TokenResponse>> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var tokenHash = tokens.HashRefreshToken(request.RefreshToken);
        var now = timeProvider.GetUtcNow();

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var currentToken = await refreshTokens.FindByHashForUpdateAsync(tokenHash, cancellationToken);

        if (currentToken is null || currentToken.ExpiresAt <= now)
        {
            return InvalidRefreshToken();
        }

        if (currentToken.UsedAt is not null || currentToken.RevokedAt is not null)
        {
            var activeFamilyTokens = await refreshTokens.ListActiveFamilyAsync(
                currentToken.FamilyId,
                cancellationToken);
            foreach (var token in activeFamilyTokens)
            {
                token.RevokedAt = now;
                token.RevokeReason = "refresh-token-reuse";
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return InvalidRefreshToken();
        }

        var user = await users.FindByIdAsync(currentToken.UserId, cancellationToken);
        if (user is null)
        {
            return InvalidRefreshToken();
        }

        var nextToken = tokens.CreateRefreshToken(
            user.Id,
            requestContext.ClientIp,
            currentToken.FamilyId,
            currentToken.Id);
        currentToken.UsedAt = now;
        currentToken.RevokedAt = now;
        currentToken.RevokeReason = "rotated";
        currentToken.ReplacedByTokenId = nextToken.Entity.Id;
        refreshTokens.Add(nextToken.Entity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var accessToken = tokens.CreateAccessToken(user);
        return ServiceResult<TokenResponse>.Ok(new TokenResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            nextToken.RawToken,
            nextToken.Entity.ExpiresAt));
    }

    public async Task<ServiceResult<bool>> LogoutAsync(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var tokenHash = tokens.HashRefreshToken(request.RefreshToken);
        var token = await refreshTokens.FindByHashAsync(tokenHash, cancellationToken);
        if (token is not null && token.RevokedAt is null)
        {
            token.RevokedAt = timeProvider.GetUtcNow();
            token.RevokeReason = "logout";
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult<bool>.NoContent();
    }

    private static AuthResponse CreateAuthResponse(
        User user,
        AccessTokenData accessToken,
        RefreshTokenData refreshToken) =>
        new(
            DtoMappings.ToCurrentUser(user),
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.RawToken,
            refreshToken.Entity.ExpiresAt);

    private static ServiceResult<AuthResponse> LoginFailed() =>
        ServiceResult<AuthResponse>.AuthenticationFailed("The login credentials are invalid.");

    private static ServiceResult<TokenResponse> InvalidRefreshToken() =>
        ServiceResult<TokenResponse>.AuthenticationFailed("Refresh token is invalid or expired.");

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
