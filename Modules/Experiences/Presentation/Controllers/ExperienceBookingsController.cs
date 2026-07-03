using Glinter.Modules.Experiences.Application.Dtos;
using Glinter.Modules.Experiences.Application.Services;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Experiences.Presentation.Controllers;

[ApiController]
[Route("api/experience-bookings")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ExperienceBookingsController : ControllerBase
{
    private readonly ExperienceService _experienceService;

    public ExperienceBookingsController(ExperienceService experienceService)
    {
        _experienceService = experienceService;
    }

    [Authorize(Roles = RoleNames.Traveler)]
    [HttpGet("my")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await _experienceService.GetMyBookingsAsync(cancellationToken));
    }

    [Authorize(Roles = RoleNames.Traveler + "," + RoleNames.ExperienceProvider + "," + RoleNames.Admin)]
    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _experienceService.CancelBookingAsync(id, cancellationToken);
            return result is null ? NotFound(new { message = "Booking not found." }) : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [Authorize(Roles = RoleNames.ExperienceProvider + "," + RoleNames.Admin)]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateExperienceBookingStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _experienceService.UpdateBookingStatusAsync(id, request.Status, cancellationToken);
            return result is null ? NotFound(new { message = "Booking not found." }) : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
