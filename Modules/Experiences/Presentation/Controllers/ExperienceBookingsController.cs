using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Commands.CancelExperienceBooking;
using Glinter.Modules.Experiences.Application.Experiences.Commands.CompleteExperienceBooking;
using Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceBooking;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceBookings;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetMyExperienceBookings;

namespace Glinter.Modules.Experiences.Presentation.Controllers;

[ApiController]
public class ExperienceBookingsController : ControllerBase
{
    private readonly CreateExperienceBookingCommandHandler _createBookingHandler;
    private readonly GetExperienceBookingsQueryHandler _getBookingsHandler;
    private readonly CancelExperienceBookingCommandHandler _cancelBookingHandler;
    private readonly CompleteExperienceBookingCommandHandler _completeBookingHandler;
    private readonly GetMyExperienceBookingsQueryHandler _getMyBookingsHandler;

    public ExperienceBookingsController(
        CreateExperienceBookingCommandHandler createBookingHandler,
        GetExperienceBookingsQueryHandler getBookingsHandler,
        GetMyExperienceBookingsQueryHandler getMyBookingsHandler,
        CancelExperienceBookingCommandHandler cancelBookingHandler,
        CompleteExperienceBookingCommandHandler completeBookingHandler)
    {
        _createBookingHandler = createBookingHandler;
        _getBookingsHandler = getBookingsHandler;
        _getMyBookingsHandler = getMyBookingsHandler;
        _cancelBookingHandler = cancelBookingHandler;
        _completeBookingHandler = completeBookingHandler;
    }

    [Authorize(Roles = RoleNames.Traveler)]
    [HttpPost("api/experiences/{experienceId:guid}/bookings")]
    public async Task<ActionResult<ExperienceBookingResponseDto>> Create(
        Guid experienceId,
        [FromBody] CreateExperienceBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _createBookingHandler.HandleAsync(
                request.ToCommand(experienceId),
                cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = "Experience was not found." });
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = RoleNames.Traveler)]
    [HttpGet("api/experience-bookings/my")]
    public async Task<ActionResult<List<ExperienceBookingResponseDto>>> GetMyBookings(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _getMyBookingsHandler.HandleAsync(
                new GetMyExperienceBookingsQuery
                {
                    Page = page,
                    PageSize = pageSize
                },
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = RoleNames.ExperienceProvider)]
    [HttpGet("api/experiences/{experienceId:guid}/bookings")]
    public async Task<ActionResult<List<ExperienceBookingResponseDto>>> GetByExperienceId(
        Guid experienceId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _getBookingsHandler.HandleAsync(
                new GetExperienceBookingsQuery
                {
                    ExperienceId = experienceId,
                    Page = page,
                    PageSize = pageSize
                },
                cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = "Experience was not found." });
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [Authorize(Roles = RoleNames.Traveler)]
    [HttpPatch("api/experience-bookings/{bookingId:guid}/cancel")]
    public async Task<ActionResult<ExperienceBookingResponseDto>> Cancel(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _cancelBookingHandler.HandleAsync(
                new CancelExperienceBookingCommand
                {
                    BookingId = bookingId
                },
                cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = "Booking was not found." });
            }

            return Ok(result);
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

    [Authorize(Roles = RoleNames.ExperienceProvider)]
    [HttpPatch("api/experience-bookings/{bookingId:guid}/complete")]
    public async Task<ActionResult<ExperienceBookingResponseDto>> Complete(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _completeBookingHandler.HandleAsync(
                new CompleteExperienceBookingCommand
                {
                    BookingId = bookingId
                },
                cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = "Booking was not found." });
            }

            return Ok(result);
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
