using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Bookings.Dtos;

namespace Glinter.Modules.Stays.Application.Bookings.Commands;

public class CancelStayBookingHandler
{
    private readonly IStayBookingRepository _stayBookingRepository;

    public CancelStayBookingHandler(IStayBookingRepository stayBookingRepository)
    {
        _stayBookingRepository = stayBookingRepository;
    }

    public async Task<StayBookingResponseDto?> HandleAsync(
        CancelStayBookingCommand command,
        CancellationToken cancellationToken = default)
    {
        var booking = await _stayBookingRepository.GetByIdAsync(command.BookingId, cancellationToken);

        if (booking is null)
            return null;

        if (booking.Status == "Cancelled")
            throw new ArgumentException("Booking is already cancelled.");

        booking.Status = "Cancelled";

        var updatedBooking = await _stayBookingRepository.UpdateAsync(booking, cancellationToken);

        return new StayBookingResponseDto
        {
            Id = updatedBooking.Id,
            StayId = updatedBooking.StayId,
            TravelerProfileId = updatedBooking.TravelerProfileId,
            CheckInDate = updatedBooking.CheckInDate,
            CheckOutDate = updatedBooking.CheckOutDate,
            GuestCount = updatedBooking.GuestCount,
            TotalPrice = updatedBooking.TotalPrice,
            Status = updatedBooking.Status,
            CreatedAtUtc = updatedBooking.CreatedAtUtc
        };
    }
}