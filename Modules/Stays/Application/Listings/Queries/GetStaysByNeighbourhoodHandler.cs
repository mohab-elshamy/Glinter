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
        if (query.Adm3Gid <= 0)
            throw new ArgumentException("Adm3Gid must be greater than 0.");

        if (query.Page < 1 || query.Page > GetStaysByNeighbourhoodQuery.MaxPage)
            throw new ArgumentException($"Page must be between 1 and {GetStaysByNeighbourhoodQuery.MaxPage}.");

        if (query.PageSize < 1 || query.PageSize > GetStaysByNeighbourhoodQuery.MaxPageSize)
            throw new ArgumentException($"PageSize must be between 1 and {GetStaysByNeighbourhoodQuery.MaxPageSize}.");

        var stays = await _stayRepository.GetByAdm3GidAsync(
            query.Adm3Gid,
            query.Page,
            query.PageSize,
            cancellationToken);

        var region = await _regionReferenceService.GetNeighbourhoodAsync(
            query.Adm3Gid,
            cancellationToken);

        return stays
            .Select(stay => Glinter.Modules.Stays.Application.Common.Mapping.StayMappings
                .ToSummaryDto(stay, region))
            .ToList();
    }
}
