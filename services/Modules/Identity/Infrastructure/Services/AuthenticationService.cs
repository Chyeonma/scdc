using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Modules.Identity.Application;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure.Persistence;
using SCDC.Modules.Identity.Infrastructure.Security;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Identity.Infrastructure.Services;

internal sealed class AuthenticationService(
    IdentityDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    IOptions<IdentityOptions> options,
    TimeProvider timeProvider,
    SessionPublisher publisher,
    IdentityRevocations revocations,
    IAuthenticatedSessionReader sessionReader) : IAuthenticationService
{
    private const string PasswordAlgorithm = "aspnetcore-identity-v3";
    private readonly IdentityOptions _options = options.Value;

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedLogin = command.Login.Trim().ToLowerInvariant();
        var userId = await dbContext.Users
            .Where(item => item.NormalizedUsername == normalizedLogin
                || item.Emails.Any(email => email.NormalizedEmail == normalizedLogin))
            .Select(item => (Guid?)item.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (userId is null)
        {
            return Result.Failure<AuthResponse>(IdentityErrors.InvalidCredentials);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.LockUserAsync(userId.Value, cancellationToken);
        var user = await dbContext.Users
            .Include(item => item.Profile)
            .Include(item => item.Emails)
            .Include(item => item.PasswordCredential)
            .Include(item => item.SecurityState)
            .SingleOrDefaultAsync(item => item.Id == userId.Value, cancellationToken);

        if (user?.PasswordCredential is null || user.SecurityState is null || user.Profile is null)
        {
            return Result.Failure<AuthResponse>(IdentityErrors.InvalidCredentials);
        }

        var now = timeProvider.GetUtcNow();
        if (user.SecurityState.LockedUntil > now)
        {
            return Result.Failure<AuthResponse>(IdentityErrors.AccountLocked(user.SecurityState.LockedUntil.Value));
        }

        var verificationResult = VerifyPassword(user, user.PasswordCredential.PasswordHash, command.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            user.SecurityState.FailedLoginCount++;
            user.SecurityState.LastFailedLoginAt = now;
            user.SecurityState.UpdatedAt = now;

            if (user.SecurityState.FailedLoginCount >= _options.MaxFailedLoginAttempts)
            {
                user.SecurityState.LockedUntil = now.AddMinutes(_options.LockoutMinutes);
            }

            dbContext.SecurityEvents.Add(IdentityData.SecurityEvent(
                user.Id,
                "login_failed",
                command.Context,
                now,
                new { reason = "invalid_password", failed_count = user.SecurityState.FailedLoginCount }));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return user.SecurityState.LockedUntil > now
                ? Result.Failure<AuthResponse>(IdentityErrors.AccountLocked(user.SecurityState.LockedUntil.Value))
                : Result.Failure<AuthResponse>(IdentityErrors.InvalidCredentials);
        }

        if (user.Status == UserStatus.PendingVerification
            || user.Emails.Single(email => email.IsPrimary).VerifiedAt is null)
        {
            return Result.Failure<AuthResponse>(IdentityErrors.EmailNotVerified);
        }

        if (user.Status != UserStatus.Active)
        {
            return Result.Failure<AuthResponse>(IdentityErrors.AccountUnavailable);
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordCredential.PasswordHash = passwordHasher.HashPassword(user, command.Password);
            user.PasswordCredential.HashAlgorithm = PasswordAlgorithm;
            user.PasswordCredential.PasswordVersion++;
            user.PasswordCredential.PasswordChangedAt = now;
            user.PasswordCredential.UpdatedAt = now;
        }

        user.SecurityState.FailedLoginCount = 0;
        user.SecurityState.LastFailedLoginAt = null;
        user.SecurityState.LockedUntil = null;
        user.SecurityState.LastSuccessfulLoginAt = now;
        user.SecurityState.UpdatedAt = now;

        var sessionExpiresAt = now.AddDays(_options.SessionDays);
        // PostgreSQL stores microseconds. Return the same deadline that refresh will read later.
        sessionExpiresAt = sessionExpiresAt.AddTicks(-(sessionExpiresAt.Ticks % 10));
        var session = new AuthSession
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            User = user,
            DeviceName = IdentityData.Truncate(command.Context.DeviceName, 100),
            UserAgent = IdentityData.Truncate(command.Context.UserAgent, 500),
            CreatedByIp = IdentityData.ParseIp(command.Context.IpAddress),
            LastSeenIp = IdentityData.ParseIp(command.Context.IpAddress),
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = sessionExpiresAt
        };
        var rawRefreshToken = tokenService.CreateOpaqueToken();
        session.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            SessionId = session.Id,
            Session = session,
            TokenHash = rawRefreshToken.Hash,
            CreatedAt = now,
            ExpiresAt = sessionExpiresAt
        });
        dbContext.AuthSessions.Add(session);
        dbContext.SecurityEvents.Add(IdentityData.SecurityEvent(
            user.Id,
            "login_succeeded",
            command.Context,
            now,
            new { session_id = session.Id }));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await publisher.PublishUserAsync(user.Id, cancellationToken);
        var response = CreateAuthResponse(user, session, rawRefreshToken.Value);
        if (!(await sessionReader.ReadAsync(new(user.Id, session.Id, user.SecurityState.SecurityStamp, response.AccessTokenExpiresAt), cancellationToken)).Allowed)
            throw new AccessCheckException("ACCESS_CHANGED");
        return Result.Success(response);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(
        RefreshSessionCommand command,
        CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashOpaqueToken(command.RefreshToken);
        var userId = await FindTokenUserIdAsync(tokenHash, cancellationToken);
        if (userId is null)
        {
            return Result.Failure<AuthResponse>(IdentityErrors.InvalidRefreshToken);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.LockUserAsync(userId.Value, cancellationToken);
        var currentToken = await dbContext.RefreshTokens
            .FromSqlInterpolated($$"""
                SELECT id, session_id, parent_token_id, replaced_by_token_id,
                       token_hash, created_at, expires_at, used_at, revoked_at, revoke_reason
                FROM identity.refresh_tokens
                WHERE token_hash = {{tokenHash}}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (currentToken is null)
        {
            return Result.Failure<AuthResponse>(IdentityErrors.InvalidRefreshToken);
        }

        var session = await dbContext.AuthSessions
            .AsSplitQuery()
            .Include(item => item.RefreshTokens)
            .Include(item => item.User)
            .ThenInclude(user => user.Profile)
            .Include(item => item.User)
            .ThenInclude(user => user.Emails)
            .Include(item => item.User)
            .ThenInclude(user => user.SecurityState)
            .SingleAsync(item => item.Id == currentToken.SessionId, cancellationToken);
        var now = timeProvider.GetUtcNow();

        if (currentToken.UsedAt is not null || currentToken.ReplacedByTokenId is not null)
        {
            await revocations.AcceptAndCompleteAsync(dbContext, session.UserId,
                new("refresh_token_reuse", "refresh_token_reuse_detected", [session.Id], command.Context), transaction, cancellationToken);
            return Result.Failure<AuthResponse>(IdentityErrors.RefreshTokenReuse);
        }

        if (currentToken.RevokedAt is not null
            || currentToken.ExpiresAt <= now
            || session.RevokedAt is not null
            || session.ExpiresAt <= now
            || session.User.Status != UserStatus.Active
            || session.User.SecurityState is null)
        {
            return Result.Failure<AuthResponse>(IdentityErrors.InvalidRefreshToken);
        }

        var rawRefreshToken = tokenService.CreateOpaqueToken();
        var nextToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            SessionId = session.Id,
            Session = session,
            ParentTokenId = currentToken.Id,
            TokenHash = rawRefreshToken.Hash,
            CreatedAt = now,
            ExpiresAt = session.ExpiresAt
        };
        currentToken.UsedAt = now;
        session.LastSeenAt = now;
        session.LastSeenIp = IdentityData.ParseIp(command.Context.IpAddress);
        dbContext.RefreshTokens.Add(nextToken);

        dbContext.SecurityEvents.Add(IdentityData.SecurityEvent(
            session.UserId,
            "refresh_token_rotated",
            command.Context,
            now,
            new { session_id = session.Id }));
        await dbContext.SaveChangesAsync(cancellationToken);

        currentToken.ReplacedByTokenId = nextToken.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await publisher.PublishUserAsync(session.UserId, cancellationToken);
        var response = CreateAuthResponse(session.User, session, rawRefreshToken.Value);
        if (!(await sessionReader.ReadAsync(new(session.UserId, session.Id, session.User.SecurityState.SecurityStamp, response.AccessTokenExpiresAt), cancellationToken)).Allowed)
            throw new AccessCheckException("ACCESS_CHANGED");
        return Result.Success(response);
    }

    public async Task<Result> LogoutAsync(
        LogoutCommand command,
        CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashOpaqueToken(command.RefreshToken);
        var userId = await FindTokenUserIdAsync(tokenHash, cancellationToken);
        if (userId is null)
        {
            return Result.Success();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.LockUserAsync(userId.Value, cancellationToken);
        var token = await dbContext.RefreshTokens
            .Include(item => item.Session)
            .ThenInclude(session => session.RefreshTokens)
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (token is null)
        {
            return Result.Success();
        }

        await revocations.AcceptAndCompleteAsync(dbContext, token.Session.UserId,
            new("user_logout", "logout", [token.SessionId], command.Context), transaction, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> LogoutAllAsync(
        Guid userId,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.LockUserAsync(userId, cancellationToken);
        var sessions = await dbContext.AuthSessions
            .Include(session => session.RefreshTokens)
            .Where(session => session.UserId == userId && session.RevokedAt == null)
            .ToListAsync(cancellationToken);
        await revocations.AcceptAndCompleteAsync(dbContext, userId,
            new("user_logout_all", "logout_all", sessions.Select(s => s.Id).ToArray(), context), transaction, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<SessionResponse>>> GetSessionsAsync(
        Guid userId,
        Guid currentSessionId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var sessions = await dbContext.AuthSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId
                && session.RevokedAt == null
                && session.ExpiresAt > now)
            .OrderByDescending(session => session.LastSeenAt)
            .Select(session => new SessionResponse(
                session.Id,
                session.DeviceName,
                session.UserAgent,
                session.LastSeenIp == null ? null : session.LastSeenIp.ToString(),
                session.CreatedAt,
                session.LastSeenAt,
                session.ExpiresAt,
                session.Id == currentSessionId))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<SessionResponse>>(sessions);
    }

    public async Task<Result> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.LockUserAsync(userId, cancellationToken);
        var session = await dbContext.AuthSessions
            .Include(item => item.RefreshTokens)
            .SingleOrDefaultAsync(
                item => item.Id == sessionId && item.UserId == userId,
                cancellationToken);
        if (session is null)
        {
            return Result.Failure(IdentityErrors.SessionNotFound);
        }

        if (session.RevokedAt is null)
        {
            await revocations.AcceptAndCompleteAsync(dbContext, userId,
                new("user_revoked_session", "session_revoked", [session.Id], context), transaction, cancellationToken);
            return Result.Success();
        }

        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    private Task<Guid?> FindTokenUserIdAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => (Guid?)token.Session.UserId)
            .SingleOrDefaultAsync(cancellationToken);

    private PasswordVerificationResult VerifyPassword(User user, string hash, string password)
    {
        try
        {
            return passwordHasher.VerifyHashedPassword(user, hash, password);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }
    }

    private AuthResponse CreateAuthResponse(User user, AuthSession session, string refreshToken)
    {
        var securityState = user.SecurityState
            ?? throw new InvalidOperationException("User security state is missing.");
        var displayName = user.Profile?.DisplayName
            ?? throw new InvalidOperationException("User profile is missing.");
        var accessToken = tokenService.CreateAccessToken(
            user,
            displayName,
            session.Id,
            securityState.SecurityStamp);

        return new AuthResponse(
            accessToken.Value,
            accessToken.ExpiresAt,
            refreshToken,
            session.ExpiresAt,
            IdentityData.ToResponse(user));
    }
}
