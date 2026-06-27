using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Domain.Enums;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.UpdateExperience;

public class UpdateExperienceCommandHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceCategoryRepository _categoryRepository;
    private readonly IVibeRepository _vibeRepository;
    private readonly IExperienceProfileResolver _profileResolver;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;
    private readonly UpdateExperienceCommandValidator _validator;

    public UpdateExperienceCommandHandler(
        IExperienceRepository experienceRepository,
        IExperienceCategoryRepository categoryRepository,
        IVibeRepository vibeRepository,
        IExperienceProfileResolver profileResolver,
        Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService regionReferenceService,
        UpdateExperienceCommandValidator validator)
    {
        _experienceRepository = experienceRepository;
        _categoryRepository = categoryRepository;
        _vibeRepository = vibeRepository;
        _profileResolver = profileResolver;
        _regionReferenceService = regionReferenceService;
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
            throw new ForbiddenException("You can update only your own experiences.");
        }

        var categoryExists = await _categoryRepository.ExistsAsync(
            command.CategoryId,
            cancellationToken);

        if (!categoryExists)
        {
            throw new NotFoundException("Experience category was not found.");
        }

        var vibesExist = await _vibeRepository.ExistsAllAsync(
            command.VibeIds,
            cancellationToken);

        if (!vibesExist)
        {
            throw new NotFoundException("One or more vibes were not found.");
        }

        var region = await _regionReferenceService.GetNeighbourhoodAsync(
            command.Adm3Gid,
            cancellationToken);

        if (region is null)
        {
            throw new NotFoundException("Adm3Gid must reference an existing neighbourhood.");
        }

        var duplicateExists = await _experienceRepository.ExistsAsync(
            providerProfileId,
            command.Title,
            command.Adm3Gid,
            experience.Id,
            cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictException(
                "An experience with the same title already exists in this area for this provider.");
        }

        experience.CategoryId = command.CategoryId;
        experience.Adm3Gid = command.Adm3Gid;
        experience.Title = command.Title.Trim();
        experience.Description = command.Description.Trim();
        experience.LocationName = command.LocationName.Trim();
        experience.PricePerPerson = command.PricePerPerson;
        experience.Currency = command.Currency.Trim().ToUpper();
        experience.DurationMinutes = command.DurationMinutes;
        experience.MaxGuests = command.MaxGuests;
        experience.Latitude = command.Latitude;
        experience.Longitude = command.Longitude;
        experience.ApprovalStatus = ExperienceApprovalStatus.Pending;
        experience.ModerationNotes = null;
        experience.ModeratedAtUtc = null;
        experience.UpdatedAtUtc = DateTime.UtcNow;

        await _experienceRepository.ReplaceTagsAsync(
            experience.Id,
            command.Tags,
            cancellationToken);

        await _experienceRepository.ReplaceVibesAsync(
            experience.Id,
            command.VibeIds,
            cancellationToken);

        await _experienceRepository.UpdateAsync(experience, cancellationToken);

        var updatedExperience = await _experienceRepository.GetByIdAsync(
            experience.Id,
            cancellationToken);

        return updatedExperience == null
            ? null
            : ExperiencesMappings.ToExperienceResponse(updatedExperience, region);
    }
}
