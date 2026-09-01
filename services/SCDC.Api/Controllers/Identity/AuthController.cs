using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Modules.Identity.Application;

namespace SCDC.Api.Controllers.Identity;

[Route("api/v1/auth")]
public sealed class AuthController(
    IRegistrationService registrationService,
    IAuthenticationService authenticationService,
    IUserAccountService userAccountService) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<RegistrationResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RegistrationResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registrationService.RegisterAsync(
            new RegisterUserCommand(
                request.Username,
                request.DisplayName,
                request.Email,
                request.Password,
                CreateRequestContext()),
            cancellationToken);
        return FromCreatedResult(result, "/api/v1/users/me");
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> VerifyEmail(
        VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registrationService.VerifyEmailAsync(
            new VerifyEmailCommand(request.Token, CreateRequestContext()),
            cancellationToken);
        return FromNoContentResult(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginAsync(
            new LoginCommand(
                request.Login,
                request.Password,
                CreateRequestContext(request.DeviceName)),
            cancellationToken);
        return FromResult(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.RefreshAsync(
            new RefreshSessionCommand(request.RefreshToken, CreateRequestContext()),
            cancellationToken);
        return FromResult(result);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.LogoutAsync(
            new LogoutCommand(request.RefreshToken, CreateRequestContext()),
            cancellationToken);
        return FromNoContentResult(result);
    }

    [Authorize]
    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var result = await authenticationService.LogoutAllAsync(
            User.GetUserId(),
            CreateRequestContext(),
            cancellationToken);
        return FromNoContentResult(result);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType<PasswordResetRequestedResponse>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<PasswordResetRequestedResponse>> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registrationService.ForgotPasswordAsync(
            new ForgotPasswordCommand(request.Email, CreateRequestContext()),
            cancellationToken);
        return FromAcceptedResult(result);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registrationService.ResetPasswordAsync(
            new ResetPasswordCommand(
                request.Token,
                request.NewPassword,
                CreateRequestContext()),
            cancellationToken);
        return FromNoContentResult(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userAccountService.ChangePasswordAsync(
            new ChangePasswordCommand(
                User.GetUserId(),
                User.GetSessionId(),
                request.CurrentPassword,
                request.NewPassword,
                CreateRequestContext()),
            cancellationToken);
        return FromNoContentResult(result);
    }

    [Authorize]
    [HttpGet("sessions")]
    [ProducesResponseType<IReadOnlyList<SessionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SessionResponse>>> GetSessions(
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.GetSessionsAsync(
            User.GetUserId(),
            User.GetSessionId(),
            cancellationToken);
        return FromResult(result);
    }

    [Authorize]
    [HttpDelete("sessions/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeSession(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.RevokeSessionAsync(
            User.GetUserId(),
            sessionId,
            CreateRequestContext(),
            cancellationToken);
        return FromNoContentResult(result);
    }

    private RequestContext CreateRequestContext(string? deviceName = null) => new(
        HttpContext.Connection.RemoteIpAddress?.ToString(),
        Request.Headers.UserAgent.ToString(),
        deviceName);
}
