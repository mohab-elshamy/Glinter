using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetMyExperiences;

public class GetMyExperiencesQueryHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceProfileResolver _profileResolver;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;

    public GetMyExperiencesQueryHandler(
        IExperienceRepository experienceRepository,
        IExperienceProfileResolver profileResolver,
        Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService regionReferenceService)
    {
        _experienceRepository = experienceRepository;
        _profileResolver = profileResolver;
        _regionReferenceService = regionReferenceService;
    }

    public async Task<List<ExperienceSummaryDto>> HandleAsync(
        GetMyExperiencesQuery query,
        CancellationToken cancellationToken = default)
    {
        var providerProfileId = await _profileResolver
            .GetCurrentExperienceProviderProfileIdAsync(cancellationToken);

        var experiences = await _experienceRepository.GetByProviderProfileIdAsync(
            providerProfileId,
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
