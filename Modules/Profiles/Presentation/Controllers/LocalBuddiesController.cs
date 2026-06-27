using System.Security.Claims;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Glinter.Modules.Profiles.Application.Profiles.Queries.GetLocalBuddies;
using Glinter.Modules.Profiles.Application.Profiles.Queries.GetUserProfileById;
using Glinter.Modules.Profiles.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Profiles.Presentation.Controllers;

[ApiController]
[Route("api/local-buddies")]
public class LocalBuddiesController : ControllerBase
{
    private readonly GetLocalBuddiesQueryHandler _getLocalBuddiesQueryHandler;
    private readonly GetUserProfileByIdQueryHandler _getUserProfileByIdQueryHandler;

    public LocalBuddiesController(
        GetLocalBuddiesQueryHandler getLocalBuddiesQueryHandler,
        GetUserProfileByIdQueryHandler getUserProfileByIdQueryHandler)
    {
        _getLocalBuddiesQueryHandler = getLocalBuddiesQueryHandler;
        _getUserProfileByIdQueryHandler = getUserProfileByIdQueryHandler;
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
}
