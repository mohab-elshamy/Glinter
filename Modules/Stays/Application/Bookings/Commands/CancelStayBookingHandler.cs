using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Bookings.Dtos;

namespace Glinter.Modules.Stays.Application.Bookings.Commands;

public class CancelStayBookingHandler
{
    private readonly IStayBookingRepository _stayBookingRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProfilesReadService _profilesReadService;

    public CancelStayBookingHandler(
        IStayBookingRepository stayBookingRepository,
        ICurrentUserService currentUserService,
        IProfilesReadService profilesReadService)
    {
        _stayBookingRepository = stayBookingRepository;
        _currentUserService = currentUserService;
        _profilesReadService = profilesReadService;
    }

    public async Task<StayBookingResponseDto?> HandleAsync(
        CancelStayBookingCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.BookingId == Guid.Empty)
            throw new ArgumentException("Booking id is required.");

        var travelerProfileId = await GetCurrentTravelerProfileIdAsync(cancellationToken);

        var booking = await _stayBookingRepository.GetByIdAsync(command.BookingId, cancellationToken);

        if (booking is null)
            return null;

        if (booking.TravelerProfileId != travelerProfileId)
            throw new UnauthorizedAccessException("You can cancel only your own bookings.");

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

    private async Task<Guid> GetCurrentTravelerProfileIdAsync(CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var travelerProfileId = await _profilesReadService.GetTravelerProfileIdByUserIdAsync(
            _currentUserService.UserId.Value,
            cancellationToken);

        return travelerProfileId
               ?? throw new UnauthorizedAccessException("Only travelers can cancel stay bookings.");
    }
}
