using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetMyExperiences;

public class GetMyExperiencesQueryHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceProfileResolver _profileResolver;

    public GetMyExperiencesQueryHandler(
        IExperienceRepository experienceRepository,
        IExperienceProfileResolver profileResolver)
    {
        _experienceRepository = experienceRepository;
        _profileResolver = profileResolver;
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

        return experiences
            .Select(ExperiencesMappings.ToExperienceSummary)
            .ToList();
    }
}