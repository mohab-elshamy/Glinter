using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Bookings.Dtos;
using Glinter.Modules.Stays.Domain.Entities;

namespace Glinter.Modules.Stays.Application.Bookings.Commands;

public class CreateStayBookingHandler
{
    private readonly IStayRepository _stayRepository;
    private readonly IStayBookingRepository _stayBookingRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProfilesReadService _profilesReadService;

    public CreateStayBookingHandler(
        IStayRepository stayRepository,
        IStayBookingRepository stayBookingRepository,
        ICurrentUserService currentUserService,
        IProfilesReadService profilesReadService)
    {
        _stayRepository = stayRepository;
        _stayBookingRepository = stayBookingRepository;
        _currentUserService = currentUserService;
        _profilesReadService = profilesReadService;
    }

    public async Task<StayBookingResponseDto> HandleAsync(
        CreateStayBookingCommand command,
        CancellationToken cancellationToken = default)


    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var currentUserId = _currentUserService.UserId.Value;

        var travelerProfileId = await _profilesReadService
            .GetTravelerProfileIdByUserIdAsync(currentUserId, cancellationToken);

        if (travelerProfileId is null)
        {
            throw new UnauthorizedAccessException("Only travelers can book stays.");
        }



        if (command.GuestCount <= 0)
            throw new ArgumentException("GuestCount must be greater than 0.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        if (command.CheckInDate < today)
            throw new ArgumentException("CheckInDate cannot be in the past.");

        if (command.CheckInDate >= command.CheckOutDate)
            throw new ArgumentException("CheckOutDate must be after CheckInDate.");

        var stay = await _stayRepository.GetByIdAsync(command.StayId, cancellationToken);

        if (stay is null)
            throw new KeyNotFoundException("Stay not found.");

        if (!stay.IsActive)
            throw new ArgumentException("This stay is not active.");

        if (command.GuestCount > stay.MaxGuests)
            throw new ArgumentException("GuestCount exceeds the maximum allowed guests for this stay.");

        var alreadyExists = await _stayBookingRepository.ExistsAsync(
            command.StayId,
            travelerProfileId.Value,
            command.CheckInDate,
            command.CheckOutDate,
            cancellationToken);

        if (alreadyExists)
            throw new ArgumentException("This booking already exists for the same traveler and dates.");

        var hasOverlap = await _stayBookingRepository.HasOverlapAsync(
            command.StayId,
            command.CheckInDate,
            command.CheckOutDate,
            cancellationToken);

        if (hasOverlap)
            throw new ArgumentException("This stay is already booked for the selected dates.");

        var nights = command.CheckOutDate.DayNumber - command.CheckInDate.DayNumber;

        if (nights <= 0)
            throw new ArgumentException("Booking must be at least one night.");

        var totalPrice = nights * stay.PricePerNight;

        var booking = new StayBooking
        {
            Id = Guid.NewGuid(),
            StayId = stay.Id,
            TravelerProfileId = travelerProfileId.Value,
            CheckInDate = command.CheckInDate,
            CheckOutDate = command.CheckOutDate,
            GuestCount = command.GuestCount,
            TotalPrice = totalPrice,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createdBooking = await _stayBookingRepository.AddAsync(booking, cancellationToken);

        return new StayBookingResponseDto
        {
            Id = createdBooking.Id,
            StayId = createdBooking.StayId,
            TravelerProfileId = createdBooking.TravelerProfileId,
            CheckInDate = createdBooking.CheckInDate,
            CheckOutDate = createdBooking.CheckOutDate,
            GuestCount = createdBooking.GuestCount,
            TotalPrice = createdBooking.TotalPrice,
            Status = createdBooking.Status,
            CreatedAtUtc = createdBooking.CreatedAtUtc
        };
    }
}