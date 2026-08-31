using ChatService.Dtos;
using ChatService.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChatService.Controllers;

[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(IAuthService authService) : ApiControllerBase
{
    [HttpPost("register")]
    public Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken) =>
        HandleAsync(authService.RegisterAsync(request, cancellationToken));

    [HttpPost("login")]
    public Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken) =>
        HandleAsync(authService.LoginAsync(request, cancellationToken));

    [HttpPost("refresh")]
    public Task<IActionResult> Refresh(
        RefreshRequest request,
        CancellationToken cancellationToken) =>
        HandleAsync(authService.RefreshAsync(request, cancellationToken));

    [HttpPost("logout")]
    public Task<IActionResult> Logout(
        RefreshRequest request,
        CancellationToken cancellationToken) =>
        HandleAsync(authService.LogoutAsync(request, cancellationToken));
}
