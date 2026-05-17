using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetVibes;

public class GetVibesQueryHandler
{
    private readonly IVibeRepository _vibeRepository;

    public GetVibesQueryHandler(IVibeRepository vibeRepository)
    {
        _vibeRepository = vibeRepository;
    }

    public async Task<List<VibeResponseDto>> HandleAsync(
        GetVibesQuery query,
        CancellationToken cancellationToken = default)
    {
        var vibes = await _vibeRepository.GetActiveAsync(cancellationToken);

        return vibes
            .Select(ExperiencesMappings.ToVibeResponse)
            .ToList();
    }
}