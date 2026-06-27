using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Profiles.Application.Profiles.Commands.UpdateLocalBuddyVerification;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Profiles.Presentation.Controllers;

[ApiController]
[Route("api/admin/local-buddies")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.Admin)]
public class AdminLocalBuddiesController : ControllerBase
{
    private readonly UpdateLocalBuddyVerificationCommandHandler _updateLocalBuddyVerificationCommandHandler;

    public AdminLocalBuddiesController(
        UpdateLocalBuddyVerificationCommandHandler updateLocalBuddyVerificationCommandHandler)
    {
        _updateLocalBuddyVerificationCommandHandler = updateLocalBuddyVerificationCommandHandler;
    }

    [HttpPatch("{userId:guid}/verification")]
    public async Task<IActionResult> UpdateVerificationStatus(
        Guid userId,
        [FromBody] UpdateLocalBuddyVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updateLocalBuddyVerificationCommandHandler.HandleAsync(
            new UpdateLocalBuddyVerificationCommand
            {
                UserId = userId,
                VerificationStatus = request.VerificationStatus
            },
            cancellationToken);

        return Ok(result);
    }
}