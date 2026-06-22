using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;

namespace Glinter.Modules.Stays.Application.Listings.Queries;

public class GetStaysByNeighbourhoodHandler
{
    private readonly IStayRepository _stayRepository;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;

    public GetStaysByNeighbourhoodHandler(
        IStayRepository stayRepository,
        Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService regionReferenceService)
    {
        _stayRepository = stayRepository;
        _regionReferenceService = regionReferenceService;
    }

    public async Task<List<StaySummaryDto>> HandleAsync(
        GetStaysByNeighbourhoodQuery query,
        CancellationToken cancellationToken = default)
    {
        var stays = await _stayRepository.GetByAdm3GidAsync(query.Adm3Gid, cancellationToken);
        var region = await _regionReferenceService.GetNeighbourhoodAsync(
            query.Adm3Gid,
            cancellationToken);

        return stays
            .Select(stay => Glinter.Modules.Stays.Application.Common.Mapping.StayMappings
                .ToSummaryDto(stay, region))
            .ToList();
    }
}
