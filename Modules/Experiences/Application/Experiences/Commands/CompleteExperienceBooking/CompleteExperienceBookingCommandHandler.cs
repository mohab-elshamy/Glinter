using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Domain.Enums;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CompleteExperienceBooking;

public class CompleteExperienceBookingCommandHandler
{
    private readonly IExperienceBookingRepository _bookingRepository;
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceAvailabilityRepository _availabilityRepository;
    private readonly IExperienceProfileResolver _profileResolver;
    private readonly CompleteExperienceBookingCommandValidator _validator;

    public CompleteExperienceBookingCommandHandler(
        IExperienceBookingRepository bookingRepository,
        IExperienceRepository experienceRepository,
        IExperienceAvailabilityRepository availabilityRepository,
        IExperienceProfileResolver profileResolver,
        CompleteExperienceBookingCommandValidator validator)
    {
        _bookingRepository = bookingRepository;
        _experienceRepository = experienceRepository;
        _availabilityRepository = availabilityRepository;
        _profileResolver = profileResolver;
        _validator = validator;
    }

    public async Task<ExperienceBookingResponseDto?> HandleAsync(
        CompleteExperienceBookingCommand command,
        CancellationToken cancellationToken = default)
    {
        _validator.Validate(command);

        var providerProfileId = await _profileResolver
            .GetCurrentExperienceProviderProfileIdAsync(cancellationToken);

        var booking = await _bookingRepository.GetForUpdateAsync(
            command.BookingId,
            cancellationToken);

        if (booking == null)
        {
            return null;
        }

        var experience = await _experienceRepository.GetByIdAsync(
            booking.ExperienceId,
            cancellationToken);

        if (experience == null)
        {
            return null;
        }

        if (experience.ProviderProfileId != providerProfileId)
        {
            throw new ForbiddenException("You can complete bookings only for your own experiences.");
        }

        if (booking.Status == ExperienceBookingStatus.Cancelled)
        {
            throw new ConflictException("Cancelled booking cannot be completed.");
        }

        if (booking.Status == ExperienceBookingStatus.Completed)
        {
            throw new ConflictException("Booking is already completed.");
        }

        var availability = await _availabilityRepository.GetByIdAsync(
            booking.AvailabilityId,
            cancellationToken);

        if (availability is null)
        {
            throw new NotFoundException("Availability slot was not found.");
        }

        if (availability.EndTimeUtc > DateTime.UtcNow)
        {
            throw new ConflictException(
                "Booking cannot be completed before the experience has ended.");
        }

        booking.Status = ExperienceBookingStatus.Completed;
        booking.CompletedAtUtc = DateTime.UtcNow;

        await _bookingRepository.UpdateAsync(booking, cancellationToken);

        return ExperiencesMappings.ToBookingResponse(booking);
    }
}
