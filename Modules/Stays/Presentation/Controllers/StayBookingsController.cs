using Glinter.Modules.Stays.Application.Bookings.Commands;
using Glinter.Modules.Stays.Application.Bookings.Dtos;
using Glinter.Modules.Stays.Application.Bookings.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Stays.Presentation.Controllers;

[ApiController]
[Route("api/stays/{stayId:guid}/bookings")]
public class StayBookingController : ControllerBase
{
    private readonly CreateStayBookingHandler _createStayBookingHandler;
    private readonly GetStayBookingsHandler _getStayBookingsHandler;
    private readonly CancelStayBookingHandler _cancelStayBookingHandler;

    public StayBookingController(
        CreateStayBookingHandler createStayBookingHandler,
        GetStayBookingsHandler getStayBookingsHandler,
        CancelStayBookingHandler cancelStayBookingHandler)
    {
        _createStayBookingHandler = createStayBookingHandler;
        _getStayBookingsHandler = getStayBookingsHandler;
        _cancelStayBookingHandler = cancelStayBookingHandler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking(
        Guid stayId,
        [FromBody] CreateStayBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateStayBookingCommand
            {
                StayId = stayId,
                TravelerProfileId = request.TravelerProfileId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                GuestCount = request.GuestCount
            };

            var result = await _createStayBookingHandler.HandleAsync(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetBookings(Guid stayId, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetStayBookingsQuery
            {
                StayId = stayId
            };

            var result = await _getStayBookingsHandler.HandleAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("~/api/stay-bookings/{bookingId:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(Guid bookingId, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CancelStayBookingCommand
            {
                BookingId = bookingId
            };

            var result = await _cancelStayBookingHandler.HandleAsync(command, cancellationToken);

            if (result is null)
                return NotFound(new { message = "Booking not found." });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}