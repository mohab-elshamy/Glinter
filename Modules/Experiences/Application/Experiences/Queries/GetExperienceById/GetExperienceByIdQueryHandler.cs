using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceById;

public class GetExperienceByIdQueryHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;

    public GetExperienceByIdQueryHandler(
        IExperienceRepository experienceRepository,
        Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService regionReferenceService)
    {
        _experienceRepository = experienceRepository;
        _regionReferenceService = regionReferenceService;
    }

    public async Task<ExperienceResponseDto?> HandleAsync(
        GetExperienceByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Id == Guid.Empty)
        {
            throw new ValidationException("Experience Id is required.");
        }

        var experience = await _experienceRepository.GetPublishedByIdAsync(
            query.Id,
            cancellationToken);

        if (experience is null)
        {
            return null;
        }

        var region = await _regionReferenceService.GetNeighbourhoodAsync(
            experience.Adm3Gid,
            cancellationToken);

        return ExperiencesMappings.ToExperienceResponse(experience, region);
    }
}
