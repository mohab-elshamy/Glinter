using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetAdminExperiences;

public class GetAdminExperiencesQueryHandler
{
    private readonly IExperienceRepository _experienceRepository;

    public GetAdminExperiencesQueryHandler(IExperienceRepository experienceRepository)
    {
        _experienceRepository = experienceRepository;
    }

    public async Task<List<ExperienceSummaryDto>> HandleAsync(
        GetAdminExperiencesQuery query,
        CancellationToken cancellationToken = default)
    {
        var experiences = await _experienceRepository.GetForAdminAsync(
            query.ApprovalStatus,
            query.IsActive,
            cancellationToken);

        return experiences
            .Select(ExperiencesMappings.ToExperienceSummary)
            .ToList();
    }
}