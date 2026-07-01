using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Stays.Application.Dtos;
using Glinter.Modules.Stays.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Stays.Presentation.Controllers;

[ApiController]
[Route("api/stay-bookings")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class StayBookingsController : ControllerBase
{
    private readonly StayService _stayService;

    public StayBookingsController(StayService stayService)
    {
        _stayService = stayService;
    }

    [Authorize(Roles = RoleNames.Traveler)]
    [HttpGet("my")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await _stayService.GetMyBookingsAsync(cancellationToken));
    }

    [Authorize(Roles = RoleNames.Traveler + "," + RoleNames.HotelOwner + "," + RoleNames.Admin)]
    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stayService.CancelBookingAsync(id, cancellationToken);
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

    [Authorize(Roles = RoleNames.HotelOwner + "," + RoleNames.Admin)]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateStayBookingStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stayService.UpdateBookingStatusAsync(id, request.Status, cancellationToken);
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
