using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Identity.Application;

public interface IUserAccountService
{
    Task<Result<UserAccountResponse>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<Result<UserAccountResponse>> UpdateProfileAsync(
        UpdateProfileCommand command,
        CancellationToken cancellationToken);

    Task<Result> ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken);
}
