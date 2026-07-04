using Glinter.Modules.Buddy.Application.Requests;
using Glinter.Modules.Buddy.Application.Requests.Dtos;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Buddy.Presentation.Controllers;

[ApiController]
[Route("api/buddy-bookings")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class BuddyCompatibilityController(BuddyRequestService service)
    : ControllerBase
{
    [HttpGet("my")]
    [Authorize(Roles = RoleNames.Traveler)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken) =>
        Ok(await service.GetMineAsync(cancellationToken));

    [HttpPatch("{requestId:guid}/status")]
    [Authorize(Roles = $"{RoleNames.LocalBuddy},{RoleNames.Admin}")]
    public async Task<IActionResult> Status(
        Guid requestId,
        [FromBody] UpdateBuddyBookingStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateLegacyStatusAsync(
            requestId, request, cancellationToken));

    [HttpPatch("{requestId:guid}/cancel")]
    [Authorize(Roles = RoleNames.Traveler)]
    public async Task<IActionResult> Cancel(
        Guid requestId,
        CancellationToken cancellationToken) =>
        Ok(await service.CancelAsync(requestId, cancellationToken));
}
