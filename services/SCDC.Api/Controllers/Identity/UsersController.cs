using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using SCDC.Modules.Identity.Application;
using SCDC.Contracts.Identity;
using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Api.Controllers.Identity;

[Authorize]
[Route("api/v1/users")]
public sealed class UsersController(IUserAccountService userAccountService, IUserSearchDirectory userSearchDirectory) : ApiControllerBase
{
    [HttpGet("search")]
    [ProducesResponseType<UserSearchPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSearchPage>> Search(
        [FromQuery, Required] string? q, [FromQuery, Range(1, 50)] int limit = 20, [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        // MVC otherwise converts an explicitly empty cursor into null; only absence
        // starts a new page. An empty supplied cursor must fail its validation.
        if (Request.Query.TryGetValue("cursor", out var suppliedCursor)) cursor = suppliedCursor.ToString();
        var result = await userSearchDirectory.SearchAsync(new UserSearchRequest(
            User.GetUserId(), User.GetSessionId(), Guid.Parse(User.FindFirst("sst")!.Value), q, limit, cursor), cancellationToken);
        if (result.Failure is not { } failure) return Ok(result.Page);
        Error error = failure.Code == "Common.Unauthorized"
            ? Error.Unauthorized(failure.Code, failure.Description)
            : new ValidationError(failure.Code, failure.Description,
                new Dictionary<string, string[]> { [failure.Field] = [failure.Description] });
        return FromResult(Result.Failure<UserSearchPage>(error));
    }

    [HttpGet("me")]
    [ProducesResponseType<UserAccountResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserAccountResponse>> GetMe(
        CancellationToken cancellationToken)
    {
        var result = await userAccountService.GetAsync(User.GetUserId(), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("me")]
    [ProducesResponseType<UserAccountResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserAccountResponse>> UpdateMe(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userAccountService.UpdateProfileAsync(
            new UpdateProfileCommand(
                User.GetUserId(),
                request.DisplayName,
                request.Bio,
                request.Locale,
                request.Timezone),
            cancellationToken);
        return FromResult(result);
    }
}
