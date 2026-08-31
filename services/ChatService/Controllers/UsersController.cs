using ChatService.Dtos;
using ChatService.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers;

[Authorize]
[Route("api/v1/users")]
public sealed class UsersController(IUserService userService) : ApiControllerBase
{
    [HttpGet("me")]
    public Task<IActionResult> GetCurrent(CancellationToken cancellationToken) =>
        HandleAsync(userService.GetCurrentAsync(cancellationToken));

    [HttpPatch("me")]
    public Task<IActionResult> UpdateCurrent(
        UpdateUserRequest request,
        CancellationToken cancellationToken) =>
        HandleAsync(userService.UpdateCurrentAsync(request, cancellationToken));
}
