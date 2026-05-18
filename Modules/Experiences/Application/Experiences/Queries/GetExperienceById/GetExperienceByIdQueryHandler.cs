using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceById;

public class GetExperienceByIdQueryHandler
{
    private readonly IExperienceRepository _experienceRepository;

    public GetExperienceByIdQueryHandler(IExperienceRepository experienceRepository)
    {
        _experienceRepository = experienceRepository;
    }

    public async Task<ExperienceResponseDto?> HandleAsync(
        GetExperienceByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Id == Guid.Empty)
        {
            throw new ArgumentException("Experience Id is required.");
        }

        var experience = await _experienceRepository.GetPublishedByIdAsync(
            query.Id,
            cancellationToken);

        return experience == null
            ? null
            : ExperiencesMappings.ToExperienceResponse(experience);
    }
}