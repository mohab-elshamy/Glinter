using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Domain.Entities;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperience;

public class CreateExperienceCommandHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceCategoryRepository _categoryRepository;
    private readonly IVibeRepository _vibeRepository;
    private readonly IExperienceProfileResolver _profileResolver;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;
    private readonly CreateExperienceCommandValidator _validator;

    public CreateExperienceCommandHandler(
        IExperienceRepository experienceRepository,
        IExperienceCategoryRepository categoryRepository,
        IVibeRepository vibeRepository,
        IExperienceProfileResolver profileResolver,
        Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService regionReferenceService,
        CreateExperienceCommandValidator validator)
    {
        _experienceRepository = experienceRepository;
        _categoryRepository = categoryRepository;
        _vibeRepository = vibeRepository;
        _profileResolver = profileResolver;
        _regionReferenceService = regionReferenceService;
        _validator = validator;
    }

    public async Task<ExperienceResponseDto> HandleAsync(
        CreateExperienceCommand command,
        CancellationToken cancellationToken = default)
    {
        _validator.Validate(command);

        var providerProfileId = await _profileResolver
            .GetCurrentExperienceProviderProfileIdAsync(cancellationToken);

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

        var region = await _regionReferenceService.GetNeighbourhoodAsync(
            command.Adm3Gid,
            cancellationToken);

        if (region is null)
        {
            throw new InvalidOperationException("Adm3Gid must reference an existing neighbourhood.");
        }

        var duplicateExists = await _experienceRepository.ExistsAsync(
            providerProfileId,
            command.Title,
            command.Adm3Gid,
            cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException("An experience with the same title already exists in this area for this provider.");
        }

        var experienceId = Guid.NewGuid();

        var experience = new Experience
        {
            Id = experienceId,
            ProviderProfileId = providerProfileId,
            CategoryId = command.CategoryId,
            Adm3Gid = command.Adm3Gid,
            Title = command.Title.Trim(),
            Description = command.Description.Trim(),
            LocationName = command.LocationName.Trim(),
            PricePerPerson = command.PricePerPerson,
            Currency = command.Currency.Trim().ToUpper(),
            DurationMinutes = command.DurationMinutes,
            MaxGuests = command.MaxGuests,
            Latitude = command.Latitude,
            Longitude = command.Longitude,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            Tags = command.Tags
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(x => new ExperienceTag
                {
                    Id = Guid.NewGuid(),
                    ExperienceId = experienceId,
                    Name = x
                })
                .ToList(),
            ExperienceVibes = command.VibeIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .Select(x => new ExperienceVibe
                {
                    ExperienceId = experienceId,
                    VibeId = x
                })
                .ToList()
        };

        await _experienceRepository.AddAsync(experience, cancellationToken);

        var createdExperience = await _experienceRepository.GetByIdAsync(
            experience.Id,
            cancellationToken);

        if (createdExperience == null)
        {
            throw new InvalidOperationException("Experience was created but could not be loaded.");
        }

        return ExperiencesMappings.ToExperienceResponse(createdExperience, region);
    }
}
