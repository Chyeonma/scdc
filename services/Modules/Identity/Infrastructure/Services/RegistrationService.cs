using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Modules.Identity.Application;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure.Persistence;
using SCDC.Modules.Identity.Infrastructure.Security;

namespace SCDC.Modules.Identity.Infrastructure.Services;

internal sealed class RegistrationService(
    IdentityDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    IOptions<IdentityOptions> options,
    TimeProvider timeProvider) : IRegistrationService
{
    private const string PasswordAlgorithm = "aspnetcore-identity-v3";
    private readonly IdentityOptions _options = options.Value;

    public async Task<Result<RegistrationResponse>> RegisterAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var validationError = IdentityValidation.ValidateRegistration(command);
        if (validationError is not null)
        {
            return Result.Failure<RegistrationResponse>(validationError);
        }

        var username = command.Username.Trim();
        var normalizedUsername = username.ToLowerInvariant();
        var email = command.Email.Trim();
        var normalizedEmail = email.ToLowerInvariant();

        if (await dbContext.Users.AnyAsync(
                user => user.NormalizedUsername == normalizedUsername,
                cancellationToken))
        {
            return Result.Failure<RegistrationResponse>(IdentityErrors.UsernameTaken);
        }

        if (await dbContext.UserEmails.AnyAsync(
                item => item.NormalizedEmail == normalizedEmail,
                cancellationToken))
        {
            return Result.Failure<RegistrationResponse>(IdentityErrors.EmailTaken);
        }

        var now = timeProvider.GetUtcNow();
        var verificationToken = tokenService.CreateOpaqueToken();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = username,
            Status = UserStatus.PendingVerification,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        };
        user.Profile = new UserProfile
        {
            UserId = user.Id,
            User = user,
            DisplayName = command.DisplayName.Trim(),
            Locale = "vi-VN",
            Timezone = "Asia/Ho_Chi_Minh",
            CreatedAt = now,
            UpdatedAt = now
        };
        user.Emails.Add(new UserEmail
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            User = user,
            Email = email,
            IsPrimary = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        user.PasswordCredential = new PasswordCredential
        {
            UserId = user.Id,
            User = user,
            PasswordHash = passwordHasher.HashPassword(user, command.Password),
            HashAlgorithm = PasswordAlgorithm,
            PasswordVersion = 1,
            PasswordChangedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        user.SecurityState = new UserSecurityState
        {
            UserId = user.Id,
            User = user,
            SecurityStamp = Guid.NewGuid(),
            UpdatedAt = now
        };
        var accountToken = new AccountToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            User = user,
            Purpose = AccountTokenPurpose.VerifyEmail,
            TokenHash = verificationToken.Hash,
            TargetValue = email,
            CreatedByIp = IdentityData.ParseIp(command.Context.IpAddress),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_options.EmailVerificationTokenMinutes)
        };
        user.AccountTokens.Add(accountToken);

        dbContext.Users.Add(user);
        dbContext.SecurityEvents.Add(IdentityData.SecurityEvent(
            user.Id,
            "registration_succeeded",
            command.Context,
            now,
            new { email_verified = false }));
        dbContext.OutboxEvents.Add(IdentityData.Outbox(
            "Identity.EmailVerificationRequested",
            user.Id,
            user.Version,
            now,
            new { user_id = user.Id, email, account_token_id = accountToken.Id }));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (TryMapUniqueViolation(exception, out var error))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<RegistrationResponse>(error);
        }

        return Result.Success(new RegistrationResponse(
            user.Id,
            user.Username,
            email,
            true,
            _options.ExposeDevelopmentTokens ? verificationToken.Value : null));
    }

    public async Task<Result> VerifyEmailAsync(
        VerifyEmailCommand command,
        CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashOpaqueToken(command.Token);
        var accountToken = await dbContext.AccountTokens
            .Include(token => token.User)
            .ThenInclude(user => user.Emails)
            .SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash
                    && token.Purpose == AccountTokenPurpose.VerifyEmail,
                cancellationToken);
        var now = timeProvider.GetUtcNow();

        if (accountToken is null
            || accountToken.ConsumedAt is not null
            || accountToken.ExpiresAt <= now
            || accountToken.User.Status is UserStatus.Deleted or UserStatus.Disabled)
        {
            return Result.Failure(IdentityErrors.InvalidAccountToken);
        }

        var email = accountToken.User.Emails.SingleOrDefault(item =>
            item.IsPrimary
            && item.Email.Equals(accountToken.TargetValue, StringComparison.OrdinalIgnoreCase));
        if (email is null)
        {
            return Result.Failure(IdentityErrors.InvalidAccountToken);
        }

        email.VerifiedAt ??= now;
        email.UpdatedAt = now;
        accountToken.ConsumedAt = now;

        if (accountToken.User.Status == UserStatus.PendingVerification)
        {
            accountToken.User.Status = UserStatus.Active;
            accountToken.User.UpdatedAt = now;
        }

        var otherTokens = await dbContext.AccountTokens
            .Where(token => token.UserId == accountToken.UserId
                && token.Purpose == AccountTokenPurpose.VerifyEmail
                && token.ConsumedAt == null
                && token.Id != accountToken.Id)
            .ToListAsync(cancellationToken);
        foreach (var token in otherTokens)
        {
            token.ConsumedAt = now;
        }

        dbContext.SecurityEvents.Add(IdentityData.SecurityEvent(
            accountToken.UserId,
            "email_verified",
            command.Context,
            now));
        dbContext.OutboxEvents.Add(IdentityData.Outbox(
            "Identity.EmailVerified",
            accountToken.UserId,
            accountToken.User.Version,
            now,
            new { user_id = accountToken.UserId, email = email.Email }));

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<PasswordResetRequestedResponse>> ForgotPasswordAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var genericResponse = new PasswordResetRequestedResponse(true, null);
        if (!IdentityValidation.IsValidEmail(command.Email))
        {
            return Result.Success(genericResponse);
        }

        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var email = await dbContext.UserEmails
            .Include(item => item.User)
            .SingleOrDefaultAsync(
                item => item.NormalizedEmail == normalizedEmail && item.IsPrimary,
                cancellationToken);

        if (email is null
            || email.User.Status is UserStatus.Deleted or UserStatus.Disabled
            || email.VerifiedAt is null)
        {
            return Result.Success(genericResponse);
        }

        var now = timeProvider.GetUtcNow();
        var rawToken = tokenService.CreateOpaqueToken();
        var activeTokens = await dbContext.AccountTokens
            .Where(token => token.UserId == email.UserId
                && token.Purpose == AccountTokenPurpose.ResetPassword
                && token.ConsumedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.ConsumedAt = now;
        }

        var accountToken = new AccountToken
        {
            Id = Guid.CreateVersion7(),
            UserId = email.UserId,
            User = email.User,
            Purpose = AccountTokenPurpose.ResetPassword,
            TokenHash = rawToken.Hash,
            TargetValue = email.Email,
            CreatedByIp = IdentityData.ParseIp(command.Context.IpAddress),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_options.PasswordResetTokenMinutes)
        };
        dbContext.AccountTokens.Add(accountToken);
        dbContext.SecurityEvents.Add(IdentityData.SecurityEvent(
            email.UserId,
            "password_reset_requested",
            command.Context,
            now));
        dbContext.OutboxEvents.Add(IdentityData.Outbox(
            "Identity.PasswordResetRequested",
            email.UserId,
            email.User.Version,
            now,
            new { user_id = email.UserId, email = email.Email, account_token_id = accountToken.Id }));

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(new PasswordResetRequestedResponse(
            true,
            _options.ExposeDevelopmentTokens ? rawToken.Value : null));
    }

    public async Task<Result> ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var validationError = IdentityValidation.ValidatePassword("newPassword", command.NewPassword);
        if (validationError is not null)
        {
            return Result.Failure(validationError);
        }

        var tokenHash = tokenService.HashOpaqueToken(command.Token);
        var accountToken = await dbContext.AccountTokens
            .Include(token => token.User)
            .ThenInclude(user => user.PasswordCredential)
            .Include(token => token.User)
            .ThenInclude(user => user.SecurityState)
            .Include(token => token.User)
            .ThenInclude(user => user.Sessions)
            .ThenInclude(session => session.RefreshTokens)
            .SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash
                    && token.Purpose == AccountTokenPurpose.ResetPassword,
                cancellationToken);
        var now = timeProvider.GetUtcNow();

        if (accountToken is null
            || accountToken.ConsumedAt is not null
            || accountToken.ExpiresAt <= now
            || accountToken.User.PasswordCredential is null
            || accountToken.User.SecurityState is null
            || accountToken.User.Status is UserStatus.Deleted or UserStatus.Disabled)
        {
            return Result.Failure(IdentityErrors.InvalidAccountToken);
        }

        accountToken.User.PasswordCredential.PasswordHash = passwordHasher.HashPassword(
            accountToken.User,
            command.NewPassword);
        accountToken.User.PasswordCredential.HashAlgorithm = PasswordAlgorithm;
        accountToken.User.PasswordCredential.PasswordVersion++;
        accountToken.User.PasswordCredential.PasswordChangedAt = now;
        accountToken.User.PasswordCredential.RequiresChange = false;
        accountToken.User.PasswordCredential.UpdatedAt = now;
        accountToken.User.SecurityState.SecurityStamp = Guid.NewGuid();
        accountToken.User.SecurityState.FailedLoginCount = 0;
        accountToken.User.SecurityState.LockedUntil = null;
        accountToken.User.SecurityState.UpdatedAt = now;
        accountToken.User.UpdatedAt = now;
        accountToken.ConsumedAt = now;

        foreach (var session in accountToken.User.Sessions)
        {
            IdentityData.RevokeSession(session, now, "password_reset");
        }

        var otherTokens = await dbContext.AccountTokens
            .Where(token => token.UserId == accountToken.UserId
                && token.Purpose == AccountTokenPurpose.ResetPassword
                && token.ConsumedAt == null
                && token.Id != accountToken.Id)
            .ToListAsync(cancellationToken);
        foreach (var token in otherTokens)
        {
            token.ConsumedAt = now;
        }

        dbContext.SecurityEvents.Add(IdentityData.SecurityEvent(
            accountToken.UserId,
            "password_reset_succeeded",
            command.Context,
            now));
        dbContext.OutboxEvents.Add(IdentityData.Outbox(
            "Identity.PasswordChanged",
            accountToken.UserId,
            accountToken.User.Version,
            now,
            new { user_id = accountToken.UserId, reason = "password_reset" }));

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static bool TryMapUniqueViolation(DbUpdateException exception, out Error error)
    {
        if (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            } postgresException)
        {
            error = postgresException.ConstraintName switch
            {
                "ux_users_normalized_username" => IdentityErrors.UsernameTaken,
                "ux_user_emails_normalized_email" => IdentityErrors.EmailTaken,
                _ => IdentityErrors.RegistrationConflict
            };
            return true;
        }

        error = Error.None;
        return false;
    }
}
