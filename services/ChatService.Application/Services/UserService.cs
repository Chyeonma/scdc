using ChatService.Dtos;
using ChatService.Services.Abstractions;

namespace ChatService.Services;

public sealed class UserService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser,
    TimeProvider timeProvider) : IUserService
{
    public async Task<ServiceResult<UserResponse>> GetCurrentAsync(
        CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(currentUser.UserId, cancellationToken, tracking: false);
        return user is null
            ? ServiceResult<UserResponse>.NotFound("User not found.")
            : ServiceResult<UserResponse>.Ok(DtoMappings.ToCurrentUser(user));
    }

    public async Task<ServiceResult<UserResponse>> UpdateCurrentAsync(
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var displayName = request.DisplayName.Trim();
        if (displayName.Length == 0)
        {
            return ServiceResult<UserResponse>.Validation(
                "displayName",
                "Display name cannot contain only whitespace.");
        }

        var user = await users.FindByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<UserResponse>.NotFound("User not found.");
        }

        user.DisplayName = displayName;
        user.UpdatedAt = timeProvider.GetUtcNow();
        user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<UserResponse>.Ok(DtoMappings.ToCurrentUser(user));
    }
}
