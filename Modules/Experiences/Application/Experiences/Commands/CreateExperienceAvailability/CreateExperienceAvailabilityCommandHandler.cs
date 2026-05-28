using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Domain.Entities;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceAvailability;

public class CreateExperienceAvailabilityCommandHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceAvailabilityRepository _availabilityRepository;
    private readonly IExperienceProfileResolver _profileResolver;
    private readonly CreateExperienceAvailabilityCommandValidator _validator;

    public CreateExperienceAvailabilityCommandHandler(
        IExperienceRepository experienceRepository,
        IExperienceAvailabilityRepository availabilityRepository,
        IExperienceProfileResolver profileResolver,
        CreateExperienceAvailabilityCommandValidator validator)
    {
        _experienceRepository = experienceRepository;
        _availabilityRepository = availabilityRepository;
        _profileResolver = profileResolver;
        _validator = validator;
    }

    public async Task<ExperienceAvailabilityResponseDto?> HandleAsync(
        CreateExperienceAvailabilityCommand command,
        CancellationToken cancellationToken = default)
    {
        _validator.Validate(command);

        var providerProfileId = await _profileResolver
            .GetCurrentExperienceProviderProfileIdAsync(cancellationToken);

        var experience = await _experienceRepository.GetByIdAsync(
            command.ExperienceId,
            cancellationToken);

        if (experience == null)
        {
            return null;
        }

        if (experience.ProviderProfileId != providerProfileId)
        {
            throw new UnauthorizedAccessException("You can add availability only to your own experiences.");
        }

        if (!experience.IsActive)
        {
            throw new InvalidOperationException("Cannot add availability to an inactive experience.");
        }

        var hasOverlap = await _availabilityRepository.HasOverlapAsync(
            command.ExperienceId,
            command.StartTimeUtc,
            command.EndTimeUtc,
            cancellationToken);

        if (hasOverlap)
        {
            throw new InvalidOperationException("This availability slot overlaps with an existing active slot.");
        }

        var availability = new ExperienceAvailability
        {
            Id = Guid.NewGuid(),
            ExperienceId = command.ExperienceId,
            StartTimeUtc = command.StartTimeUtc,
            EndTimeUtc = command.EndTimeUtc,
            Capacity = command.Capacity,
            BookedCount = 0,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _availabilityRepository.AddAsync(availability, cancellationToken);

        return ExperiencesMappings.ToAvailabilityResponse(availability);
    }
}