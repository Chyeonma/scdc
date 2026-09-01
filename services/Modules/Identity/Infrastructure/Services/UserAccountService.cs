using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Modules.Identity.Application;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Modules.Identity.Infrastructure.Services;

internal sealed class UserAccountService(
    IdentityDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    TimeProvider timeProvider) : IUserAccountService
{
    private const string PasswordAlgorithm = "aspnetcore-identity-v3";

    public async Task<Result<UserAccountResponse>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await LoadUserAsync(userId, cancellationToken);
        return user is null
            ? Result.Failure<UserAccountResponse>(IdentityErrors.UserNotFound)
            : Result.Success(IdentityData.ToResponse(user));
    }

    public async Task<Result<UserAccountResponse>> UpdateProfileAsync(
        UpdateProfileCommand command,
        CancellationToken cancellationToken)
    {
        var validationError = IdentityValidation.ValidateProfile(command);
        if (validationError is not null)
        {
            return Result.Failure<UserAccountResponse>(validationError);
        }

        var user = await LoadUserAsync(command.UserId, cancellationToken);
        if (user?.Profile is null)
        {
            return Result.Failure<UserAccountResponse>(IdentityErrors.UserNotFound);
        }

        var now = timeProvider.GetUtcNow();
        user.Profile.DisplayName = command.DisplayName.Trim();
        user.Profile.Bio = string.IsNullOrWhiteSpace(command.Bio) ? null : command.Bio.Trim();
        user.Profile.Locale = command.Locale.Trim();
        user.Profile.Timezone = command.Timezone.Trim();
        user.Profile.UpdatedAt = now;
        user.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(IdentityData.ToResponse(user));
    }

    public async Task<Result> ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        var validationError = IdentityValidation.ValidatePassword("newPassword", command.NewPassword);
        if (validationError is not null)
        {
            return Result.Failure(validationError);
        }

        var user = await dbContext.Users
            .Include(item => item.PasswordCredential)
            .Include(item => item.SecurityState)
            .Include(item => item.Sessions)
            .ThenInclude(session => session.RefreshTokens)
            .SingleOrDefaultAsync(item => item.Id == command.UserId, cancellationToken);
        if (user?.PasswordCredential is null || user.SecurityState is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        var currentPasswordResult = VerifyPassword(
            user,
            user.PasswordCredential.PasswordHash,
            command.CurrentPassword);
        if (currentPasswordResult == PasswordVerificationResult.Failed)
        {
            return Result.Failure(new ValidationError(
                "Identity.CurrentPasswordInvalid",
                "The current password is invalid.",
                new Dictionary<string, string[]>
                {
                    ["currentPassword"] = ["The current password is invalid."]
                }));
        }

        if (VerifyPassword(user, user.PasswordCredential.PasswordHash, command.NewPassword)
            != PasswordVerificationResult.Failed)
        {
            return Result.Failure(new ValidationError(
                "Identity.PasswordUnchanged",
                "The new password must be different from the current password.",
                new Dictionary<string, string[]>
                {
                    ["newPassword"] = ["The new password must be different from the current password."]
                }));
        }

        var now = timeProvider.GetUtcNow();
        user.PasswordCredential.PasswordHash = passwordHasher.HashPassword(user, command.NewPassword);
        user.PasswordCredential.HashAlgorithm = PasswordAlgorithm;
        user.PasswordCredential.PasswordVersion++;
        user.PasswordCredential.PasswordChangedAt = now;
        user.PasswordCredential.RequiresChange = false;
        user.PasswordCredential.UpdatedAt = now;
        user.SecurityState.SecurityStamp = Guid.NewGuid();
        user.SecurityState.FailedLoginCount = 0;
        user.SecurityState.LockedUntil = null;
        user.SecurityState.UpdatedAt = now;
        user.UpdatedAt = now;

        foreach (var session in user.Sessions)
        {
            IdentityData.RevokeSession(session, now, "password_changed");
        }

        dbContext.SecurityEvents.Add(IdentityData.SecurityEvent(
            user.Id,
            "password_changed",
            command.Context,
            now,
            new { session_id = command.SessionId }));
        dbContext.OutboxEvents.Add(IdentityData.Outbox(
            "Identity.PasswordChanged",
            user.Id,
            user.Version,
            now,
            new { user_id = user.Id, reason = "password_changed" }));

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<User?> LoadUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Users
            .Include(item => item.Profile)
            .Include(item => item.Emails)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

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
}
