using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.UpdateExperience;

public class UpdateExperienceCommandHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceCategoryRepository _categoryRepository;
    private readonly IVibeRepository _vibeRepository;
    private readonly IExperienceProfileResolver _profileResolver;
    private readonly UpdateExperienceCommandValidator _validator;

    public UpdateExperienceCommandHandler(
        IExperienceRepository experienceRepository,
        IExperienceCategoryRepository categoryRepository,
        IVibeRepository vibeRepository,
        IExperienceProfileResolver profileResolver,
        UpdateExperienceCommandValidator validator)
    {
        _experienceRepository = experienceRepository;
        _categoryRepository = categoryRepository;
        _vibeRepository = vibeRepository;
        _profileResolver = profileResolver;
        _validator = validator;
    }

    public async Task<ExperienceResponseDto?> HandleAsync(
        UpdateExperienceCommand command,
        CancellationToken cancellationToken = default)
    {
        _validator.Validate(command);

        var providerProfileId = await _profileResolver
            .GetCurrentExperienceProviderProfileIdAsync(cancellationToken);

        var experience = await _experienceRepository.GetForUpdateAsync(
            command.Id,
            cancellationToken);

        if (experience == null)
        {
            return null;
        }

        if (experience.ProviderProfileId != providerProfileId)
        {
            throw new UnauthorizedAccessException("You can update only your own experiences.");
        }

        var categoryExists = await _categoryRepository.ExistsAsync(
            command.CategoryId,
            cancellationToken);

        if (!categoryExists)
        {
            throw new InvalidOperationException("Experience category was not found.");
        }

        var vibesExist = await _vibeRepository.ExistsAllAsync(
            command.VibeIds,
            cancellationToken);

        if (!vibesExist)
        {
            throw new InvalidOperationException("One or more vibes were not found.");
        }

        experience.CategoryId = command.CategoryId;
        experience.AreaId = command.AreaId;
        experience.Title = command.Title.Trim();
        experience.Description = command.Description.Trim();
        experience.LocationName = command.LocationName.Trim();
        experience.PricePerPerson = command.PricePerPerson;
        experience.Currency = command.Currency.Trim().ToUpper();
        experience.DurationMinutes = command.DurationMinutes;
        experience.MaxGuests = command.MaxGuests;
        experience.Latitude = command.Latitude;
        experience.Longitude = command.Longitude;
        experience.UpdatedAtUtc = DateTime.UtcNow;

        await _experienceRepository.UpdateAsync(experience, cancellationToken);

        await _experienceRepository.ReplaceTagsAsync(
            experience.Id,
            command.Tags,
            cancellationToken);

        await _experienceRepository.ReplaceVibesAsync(
            experience.Id,
            command.VibeIds,
            cancellationToken);

        var updatedExperience = await _experienceRepository.GetByIdAsync(
            experience.Id,
            cancellationToken);

        return updatedExperience == null
            ? null
            : ExperiencesMappings.ToExperienceResponse(updatedExperience);
    }
}