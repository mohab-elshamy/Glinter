using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.ActivateExperienceAvailability;

public class ActivateExperienceAvailabilityCommandHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceAvailabilityRepository _availabilityRepository;
    private readonly IExperienceProfileResolver _profileResolver;
    private readonly ActivateExperienceAvailabilityCommandValidator _validator;

    public ActivateExperienceAvailabilityCommandHandler(
        IExperienceRepository experienceRepository,
        IExperienceAvailabilityRepository availabilityRepository,
        IExperienceProfileResolver profileResolver,
        ActivateExperienceAvailabilityCommandValidator validator)
    {
        _experienceRepository = experienceRepository;
        _availabilityRepository = availabilityRepository;
        _profileResolver = profileResolver;
        _validator = validator;
    }

    public async Task<ExperienceAvailabilityResponseDto?> HandleAsync(
        ActivateExperienceAvailabilityCommand command,
        CancellationToken cancellationToken = default)
    {
        _validator.Validate(command);

        var providerProfileId = await _profileResolver
            .GetCurrentExperienceProviderProfileIdAsync(cancellationToken);

        var availability = await _availabilityRepository.GetForUpdateAsync(
            command.AvailabilityId,
            cancellationToken);

        if (availability == null)
        {
            return null;
        }

        var experience = await _experienceRepository.GetByIdAsync(
            availability.ExperienceId,
            cancellationToken);

        if (experience == null)
        {
            return null;
        }

        if (experience.ProviderProfileId != providerProfileId)
        {
            throw new ForbiddenException("You can activate availability only for your own experiences.");
        }

        if (!experience.IsActive)
        {
            throw new ConflictException("Cannot activate availability for an inactive experience.");
        }

        if (availability.StartTimeUtc <= DateTime.UtcNow)
        {
            throw new ConflictException("Cannot activate an availability slot in the past.");
        }

        if (availability.BookedCount > availability.Capacity)
        {
            throw new ConflictException("Availability booked count cannot be greater than capacity.");
        }

        if (availability.IsActive)
        {
            return ExperiencesMappings.ToAvailabilityResponse(availability);
        }

        var hasOverlap = await _availabilityRepository.HasOverlapAsync(
            availability.ExperienceId,
            availability.StartTimeUtc,
            availability.EndTimeUtc,
            cancellationToken);

        if (hasOverlap)
        {
            throw new ConflictException("Cannot activate this slot because it overlaps with another active slot.");
        }

        availability.IsActive = true;

        await _availabilityRepository.UpdateAsync(availability, cancellationToken);

        return ExperiencesMappings.ToAvailabilityResponse(availability);
    }
}