using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Identity.Application;

public interface IRegistrationService
{
    Task<Result<RegistrationResponse>> RegisterAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken);

    Task<Result> VerifyEmailAsync(
        VerifyEmailCommand command,
        CancellationToken cancellationToken);

    Task<Result<PasswordResetRequestedResponse>> ForgotPasswordAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken);

    Task<Result> ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken);
}
