using Glinter.Modules.Buddy.Application.Requests;
using Glinter.Modules.Buddy.Application.Requests.Dtos;
using Glinter.Modules.Buddy.Application.Reviews.Dtos;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Buddy.Presentation.Controllers;

[ApiController]
[Route("api/local-buddies")]
public sealed class LocalBuddyEngagementController(BuddyRequestService service)
    : ControllerBase
{
    [HttpGet("{userId:guid}/availability")]
    public async Task<IActionResult> Availability(
        Guid userId,
        CancellationToken cancellationToken) =>
        Ok(await service.GetAvailabilityAsync(
            userId, false, cancellationToken));

    [HttpGet("{userId:guid}/availability/manage")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.LocalBuddy)]
    public async Task<IActionResult> ManagedAvailability(
        Guid userId,
        CancellationToken cancellationToken) =>
        Ok(await service.GetAvailabilityAsync(
            userId, true, cancellationToken));

    [HttpPost("{userId:guid}/availability")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.LocalBuddy)]
    public async Task<IActionResult> CreateAvailability(
        Guid userId,
        [FromBody] BuddyAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAvailabilityAsync(
            userId, request, cancellationToken);
        return Created(
            $"/api/local-buddies/{userId}/availability/{result.Id}",
            result);
    }

    [HttpPost("{userId:guid}/bookings")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.Traveler)]
    public async Task<IActionResult> CreateBooking(
        Guid userId,
        [FromBody] CreateBuddyBookingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateLegacyRequestAsync(
            userId, request, cancellationToken);
        return Created($"/api/buddy/requests/{result.Id}", result);
    }

    [HttpGet("{userId:guid}/bookings")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Bookings(
        Guid userId,
        CancellationToken cancellationToken) =>
        Ok(await service.GetBuddyRequestsAsync(userId, cancellationToken));

    [HttpGet("{userId:guid}/reviews")]
    public async Task<IActionResult> Reviews(
        Guid userId,
        CancellationToken cancellationToken) =>
        Ok(await service.GetReviewsAsync(userId, cancellationToken));

    [HttpGet("{userId:guid}/review-summary")]
    public async Task<IActionResult> ReviewSummary(
        Guid userId,
        CancellationToken cancellationToken) =>
        Ok(await service.GetReviewSummaryAsync(userId, cancellationToken));

    [HttpPost("{userId:guid}/reviews")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.Traveler)]
    public async Task<IActionResult> CreateReview(
        Guid userId,
        [FromBody] LegacyCreateBuddyReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateLegacyReviewAsync(
            userId, request, cancellationToken);
        return Created(
            $"/api/local-buddies/{userId}/reviews/{result.Id}",
            result);
    }
}
