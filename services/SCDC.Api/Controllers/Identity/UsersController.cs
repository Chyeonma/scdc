using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Modules.Identity.Application;

namespace SCDC.Api.Controllers.Identity;

[Authorize]
[Route("api/v1/users")]
public sealed class UsersController(IUserAccountService userAccountService) : ApiControllerBase
{
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
