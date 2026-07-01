using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Glinter.Modules.Profiles.Application.Profiles.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Profiles.Presentation.Controllers;

[ApiController]
[Route("api/buddy-bookings")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class BuddyBookingsController(BuddyEngagementService service) : ControllerBase
{
    [HttpGet("my")]
    [Authorize(Roles = RoleNames.Traveler)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken) =>
        Ok(await service.GetMyBookingsAsync(cancellationToken));

    [HttpPatch("{bookingId:guid}/status")]
    [Authorize(Roles = $"{RoleNames.LocalBuddy},{RoleNames.Admin}")]
    public async Task<IActionResult> UpdateStatus(
        Guid bookingId,
        [FromBody] UpdateBuddyBookingStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateBookingStatusAsync(
            bookingId,
            request,
            cancellationToken));

    [HttpPatch("{bookingId:guid}/cancel")]
    [Authorize(Roles = RoleNames.Traveler)]
    public async Task<IActionResult> Cancel(
        Guid bookingId,
        CancellationToken cancellationToken) =>
        Ok(await service.CancelBookingAsync(
            bookingId,
            cancellationToken));
}
