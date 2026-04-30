using Glinter.Modules.Profiles.Application.Profiles.Queries.GetLocalBuddies;
using Glinter.Modules.Profiles.Application.Profiles.Queries.GetUserProfileById;
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
        CancellationToken cancellationToken)
    {
        var result = await _getLocalBuddiesQueryHandler.HandleAsync(
            new GetLocalBuddiesQuery
            {
                City = city
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetLocalBuddyByUserId(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getUserProfileByIdQueryHandler.HandleAsync(
                new GetUserProfileByIdQuery
                {
                    UserId = userId
                },
                cancellationToken);

            if (result.ProfileType != "LocalBuddy")
                return NotFound(new { message = "Local buddy profile not found." });

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}