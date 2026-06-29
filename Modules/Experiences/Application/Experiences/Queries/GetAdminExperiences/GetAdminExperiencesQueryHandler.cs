using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetAdminExperiences;

public class GetAdminExperiencesQueryHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;

    public GetAdminExperiencesQueryHandler(
        IExperienceRepository experienceRepository,
        Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService regionReferenceService)
    {
        _experienceRepository = experienceRepository;
        _regionReferenceService = regionReferenceService;
    }

    public async Task<List<ExperienceSummaryDto>> HandleAsync(
        GetAdminExperiencesQuery query,
        CancellationToken cancellationToken = default)
    {
        Glinter.Modules.Experiences.Application.Common.ExperiencePagination.Validate(
            query.Page,
            query.PageSize);

        var experiences = await _experienceRepository.GetForAdminAsync(
            query.ApprovalStatus,
            query.IsActive,
            query.Page,
            query.PageSize,
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
