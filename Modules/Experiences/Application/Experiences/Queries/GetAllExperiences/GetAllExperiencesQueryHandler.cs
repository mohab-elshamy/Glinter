using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetAllExperiences;

public class GetAllExperiencesQueryHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;

    public GetAllExperiencesQueryHandler(
        IExperienceRepository experienceRepository,
        Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService regionReferenceService)
    {
        _experienceRepository = experienceRepository;
        _regionReferenceService = regionReferenceService;
    }

    public async Task<List<ExperienceSummaryDto>> HandleAsync(
        GetAllExperiencesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.MinPrice.HasValue && query.MinPrice.Value < 0)
        {
            throw new ArgumentException("MinPrice cannot be negative.");
        }

        if (query.MaxPrice.HasValue && query.MaxPrice.Value < 0)
        {
            throw new ArgumentException("MaxPrice cannot be negative.");
        }

        if (query.MinPrice.HasValue &&
            query.MaxPrice.HasValue &&
            query.MinPrice.Value > query.MaxPrice.Value)
        {
            throw new ArgumentException("MinPrice cannot be greater than MaxPrice.");
        }

        if (query.Guests.HasValue && query.Guests.Value <= 0)
        {
            throw new ArgumentException("Guests must be greater than zero.");
        }

        var experiences = await _experienceRepository.GetFilteredAsync(
            query.Adm3Gid,
            query.CategoryId,
            query.MinPrice,
            query.MaxPrice,
            query.Guests,
            query.VibeId,
            query.Tag,
            cancellationToken);

        var regions = await _regionReferenceService.GetNeighbourhoodsAsync(
            experiences.Select(x => x.Adm3Gid),
            cancellationToken);

        return experiences
            .Select(experience => ExperiencesMappings.ToExperienceSummary(
                experience,
                regions.GetValueOrDefault(experience.Adm3Gid)))
            .ToList();
    }
}
