using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Domain.Enums;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CancelExperienceBooking;

public class CancelExperienceBookingCommandHandler
{
    private readonly IExperienceBookingRepository _bookingRepository;
    private readonly IExperienceAvailabilityRepository _availabilityRepository;
    private readonly IExperienceProfileResolver _profileResolver;
    private readonly CancelExperienceBookingCommandValidator _validator;

    public CancelExperienceBookingCommandHandler(
        IExperienceBookingRepository bookingRepository,
        IExperienceAvailabilityRepository availabilityRepository,
        IExperienceProfileResolver profileResolver,
        CancelExperienceBookingCommandValidator validator)
    {
        _bookingRepository = bookingRepository;
        _availabilityRepository = availabilityRepository;
        _profileResolver = profileResolver;
        _validator = validator;
    }

    public async Task<ExperienceBookingResponseDto?> HandleAsync(
        CancelExperienceBookingCommand command,
        CancellationToken cancellationToken = default)
    {
        _validator.Validate(command);

        var travelerProfileId = await _profileResolver
            .GetCurrentTravelerProfileIdAsync(cancellationToken);

        var booking = await _bookingRepository.GetForUpdateAsync(
            command.BookingId,
            cancellationToken);

        if (booking == null)
        {
            return null;
        }

        if (booking.TravelerProfileId != travelerProfileId)
        {
            throw new UnauthorizedAccessException("You can cancel only your own bookings.");
        }

        if (booking.Status == ExperienceBookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Booking is already cancelled.");
        }

        if (booking.Status == ExperienceBookingStatus.Completed)
        {
            throw new InvalidOperationException("Completed booking cannot be cancelled.");
        }

        booking.Status = ExperienceBookingStatus.Cancelled;
        booking.CancelledAtUtc = DateTime.UtcNow;

        var availability = await _availabilityRepository.GetForUpdateAsync(
            booking.AvailabilityId,
            cancellationToken);

        if (availability == null)
        {
            throw new InvalidOperationException("Availability slot was not found.");
        }

        availability.BookedCount = Math.Max(0, availability.BookedCount - booking.GuestsCount);

        await _bookingRepository.UpdateBookingAndAvailabilityAsync(
            booking,
            availability,
            cancellationToken);

        return ExperiencesMappings.ToBookingResponse(booking);
    }
}