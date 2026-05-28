using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.DeactivateExperienceAvailability;

public class DeactivateExperienceAvailabilityCommandHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceAvailabilityRepository _availabilityRepository;
    private readonly IExperienceProfileResolver _profileResolver;
    private readonly DeactivateExperienceAvailabilityCommandValidator _validator;

    public DeactivateExperienceAvailabilityCommandHandler(
        IExperienceRepository experienceRepository,
        IExperienceAvailabilityRepository availabilityRepository,
        IExperienceProfileResolver profileResolver,
        DeactivateExperienceAvailabilityCommandValidator validator)
    {
        _experienceRepository = experienceRepository;
        _availabilityRepository = availabilityRepository;
        _profileResolver = profileResolver;
        _validator = validator;
    }

    public async Task<ExperienceAvailabilityResponseDto?> HandleAsync(
        DeactivateExperienceAvailabilityCommand command,
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
            throw new UnauthorizedAccessException("You can deactivate availability only for your own experiences.");
        }

        availability.IsActive = false;

        await _availabilityRepository.UpdateAsync(availability, cancellationToken);

        return ExperiencesMappings.ToAvailabilityResponse(availability);
    }
}