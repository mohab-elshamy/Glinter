using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Stays.Application.Bookings.Commands;
using Glinter.Modules.Stays.Application.Bookings.Dtos;
using Glinter.Modules.Stays.Application.Bookings.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

    [Authorize(Roles = RoleNames.Traveler)]
    [HttpPost("/api/stays/{stayId:guid}/bookings")]
    public async Task<IActionResult> Create(
        Guid stayId,
        [FromBody] CreateStayBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateStayBookingCommand
        {
            StayId = stayId,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            GuestCount = request.GuestCount
        };

        var result = await _createStayBookingHandler.HandleAsync(command, cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = RoleNames.HotelOwner)]
    [HttpGet]
    public async Task<IActionResult> GetBookings(Guid stayId, CancellationToken cancellationToken)
    {
        var query = new GetStayBookingsQuery
        {
            StayId = stayId
        };

        var result = await _getStayBookingsHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = RoleNames.Traveler)]
    [HttpPatch("~/api/stay-bookings/{bookingId:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(Guid bookingId, CancellationToken cancellationToken)
    {
        var command = new CancelStayBookingCommand
        {
            BookingId = bookingId
        };

        var result = await _cancelStayBookingHandler.HandleAsync(command, cancellationToken);

        if (result is null)
            throw new NotFoundException("Booking not found.");

        return Ok(result);
    }
}
