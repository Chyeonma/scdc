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
    TimeProvider timeProvider,
    IdentityRevocations revocations) : IUserAccountService
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

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.LockUserAsync(command.UserId, cancellationToken);
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
            return Result.Failure(IdentityErrors.CurrentPasswordInvalid);
        }

        if (VerifyPassword(user, user.PasswordCredential.PasswordHash, command.NewPassword)
            != PasswordVerificationResult.Failed)
        {
            return Result.Failure(IdentityErrors.PasswordUnchanged);
        }

        var hash = passwordHasher.HashPassword(user, command.NewPassword);
        await revocations.AcceptAndCompleteAsync(dbContext, user.Id,
            new("password_changed", "password_changed", user.Sessions.Where(s => s.RevokedAt == null).Select(s => s.Id).ToArray(),
                command.Context, PasswordHash: hash, RequestSessionId: command.SessionId), transaction, cancellationToken);
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
