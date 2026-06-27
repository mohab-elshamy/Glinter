using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Domain.Entities;
using Glinter.Modules.Experiences.Domain.Enums;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceBooking;

public class CreateExperienceBookingCommandHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceAvailabilityRepository _availabilityRepository;
    private readonly IExperienceBookingRepository _bookingRepository;
    private readonly IExperienceProfileResolver _profileResolver;
    private readonly CreateExperienceBookingCommandValidator _validator;

    public CreateExperienceBookingCommandHandler(
        IExperienceRepository experienceRepository,
        IExperienceAvailabilityRepository availabilityRepository,
        IExperienceBookingRepository bookingRepository,
        IExperienceProfileResolver profileResolver,
        CreateExperienceBookingCommandValidator validator)
    {
        _experienceRepository = experienceRepository;
        _availabilityRepository = availabilityRepository;
        _bookingRepository = bookingRepository;
        _profileResolver = profileResolver;
        _validator = validator;
    }

    public async Task<ExperienceBookingResponseDto?> HandleAsync(
        CreateExperienceBookingCommand command,
        CancellationToken cancellationToken = default)
    {
        _validator.Validate(command);

        var travelerProfileId = await _profileResolver
            .GetCurrentTravelerProfileIdAsync(cancellationToken);

        var experience = await _experienceRepository.GetByIdAsync(
            command.ExperienceId,
            cancellationToken);

        if (experience == null)
        {
            return null;
        }

        if (!experience.IsActive || experience.ApprovalStatus != ExperienceApprovalStatus.Approved)
        {
            throw new ConflictException("Cannot book an experience that is not active and approved.");
        }

        if (command.GuestsCount > experience.MaxGuests)
        {
            throw new ValidationException("GuestsCount exceeds the maximum guests allowed for this experience.");
        }

        var availability = await _availabilityRepository.GetForUpdateAsync(
            command.AvailabilityId,
            cancellationToken);

        if (availability == null)
        {
            throw new NotFoundException("Availability slot was not found.");
        }

        if (availability.ExperienceId != command.ExperienceId)
        {
            throw new ConflictException("Availability slot does not belong to this experience.");
        }

        if (!availability.IsActive)
        {
            throw new ConflictException("Availability slot is not active.");
        }

        if (availability.StartTimeUtc <= DateTime.UtcNow)
        {
            throw new ConflictException("Cannot book an availability slot in the past.");
        }

        var remainingCapacity = availability.Capacity - availability.BookedCount;

        if (command.GuestsCount > remainingCapacity)
        {
            throw new ConflictException("Not enough remaining capacity for this availability slot.");
        }

        var alreadyBooked = await _bookingRepository.HasActiveBookingAsync(
            command.AvailabilityId,
            travelerProfileId,
            cancellationToken);

        if (alreadyBooked)
        {
            throw new ConflictException("You already have an active booking for this availability slot.");
        }

        var booking = new ExperienceBooking
        {
            Id = Guid.NewGuid(),
            ExperienceId = command.ExperienceId,
            AvailabilityId = command.AvailabilityId,
            TravelerProfileId = travelerProfileId,
            GuestsCount = command.GuestsCount,
            TotalPrice = experience.PricePerPerson * command.GuestsCount,
            Status = ExperienceBookingStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _bookingRepository.AddBookingAndUpdateAvailabilityAsync(
            booking,
            availability,
            cancellationToken);

        return ExperiencesMappings.ToBookingResponse(booking);
    }
}
