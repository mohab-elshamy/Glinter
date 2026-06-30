using System.Security.Claims;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Glinter.Modules.Profiles.Application.Profiles.Queries.GetLocalBuddies;
using Glinter.Modules.Profiles.Application.Profiles.Queries.GetUserProfileById;
using Glinter.Modules.Profiles.Domain.Enums;
using Glinter.Modules.Profiles.Application.Profiles.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Profiles.Presentation.Controllers;

[ApiController]
[Route("api/local-buddies")]
public class LocalBuddiesController : ControllerBase
{
    private readonly GetLocalBuddiesQueryHandler _getLocalBuddiesQueryHandler;
    private readonly GetUserProfileByIdQueryHandler _getUserProfileByIdQueryHandler;
    private readonly BuddyEngagementService _buddyEngagementService;

    public LocalBuddiesController(
        GetLocalBuddiesQueryHandler getLocalBuddiesQueryHandler,
        GetUserProfileByIdQueryHandler getUserProfileByIdQueryHandler,
        BuddyEngagementService buddyEngagementService)
    {
        _getLocalBuddiesQueryHandler = getLocalBuddiesQueryHandler;
        _getUserProfileByIdQueryHandler = getUserProfileByIdQueryHandler;
        _buddyEngagementService = buddyEngagementService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLocalBuddies(
        [FromQuery] string? city,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _getLocalBuddiesQueryHandler.HandleAsync(
            new GetLocalBuddiesQuery
            {
                City = city,
                Search = search,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetLocalBuddyByUserId(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _getUserProfileByIdQueryHandler.HandleAsync(
            new GetUserProfileByIdQuery
            {
                UserId = userId
            },
            cancellationToken);

        if (result is not LocalBuddyProfileResponse localBuddy)
            throw new NotFoundException("Local buddy profile not found.");

        var isApproved = string.Equals(
            localBuddy.VerificationStatus,
            VerificationStatus.Approved.ToString(),
            StringComparison.OrdinalIgnoreCase);
        var isOwner = Guid.TryParse(
                          User.FindFirstValue(ClaimTypes.NameIdentifier),
                          out var currentUserId) &&
                      currentUserId == userId;
        var isAdmin = User.IsInRole(RoleNames.Admin);

        if (!isApproved && !isOwner && !isAdmin)
            throw new NotFoundException("Local buddy profile not found.");

        return Ok(localBuddy);
    }

    [HttpGet("{userId:guid}/availability")]
    public async Task<IActionResult> GetAvailability(
        Guid userId,
        CancellationToken cancellationToken) =>
        Ok(await _buddyEngagementService.GetAvailabilityAsync(
            userId,
            manage: false,
            cancellationToken));

    [HttpGet("{userId:guid}/availability/manage")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.LocalBuddy)]
    public async Task<IActionResult> GetManagedAvailability(
        Guid userId,
        CancellationToken cancellationToken) =>
        Ok(await _buddyEngagementService.GetAvailabilityAsync(
            userId,
            manage: true,
            cancellationToken));

    [HttpPost("{userId:guid}/availability")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.LocalBuddy)]
    public async Task<IActionResult> CreateAvailability(
        Guid userId,
        [FromBody] BuddyAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buddyEngagementService.CreateAvailabilityAsync(
            userId,
            request,
            cancellationToken);
        return CreatedAtAction(nameof(GetAvailability), new { userId }, result);
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
        var result = await _buddyEngagementService.CreateBookingAsync(
            userId,
            request,
            cancellationToken);
        return Created($"/api/buddy-bookings/{result.Id}", result);
    }

    [HttpGet("{userId:guid}/bookings")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> GetBookings(
        Guid userId,
        CancellationToken cancellationToken) =>
        Ok(await _buddyEngagementService.GetBuddyBookingsAsync(
            userId,
            cancellationToken));

    [HttpGet("{userId:guid}/reviews")]
    public async Task<IActionResult> GetReviews(
        Guid userId,
        CancellationToken cancellationToken) =>
        Ok(await _buddyEngagementService.GetReviewsAsync(
            userId,
            cancellationToken));

    [HttpPost("{userId:guid}/reviews")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.Traveler)]
    public async Task<IActionResult> CreateReview(
        Guid userId,
        [FromBody] CreateBuddyReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buddyEngagementService.CreateReviewAsync(
            userId,
            request,
            cancellationToken);
        return Created($"/api/local-buddies/{userId}/reviews/{result.Id}", result);
    }
}
