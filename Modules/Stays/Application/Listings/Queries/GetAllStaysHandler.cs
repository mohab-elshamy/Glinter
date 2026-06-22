using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;

namespace Glinter.Modules.Stays.Application.Listings.Queries;

public class GetAllStaysHandler
{
    private readonly IStayRepository _stayRepository;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;

    public GetAllStaysHandler(
        IStayRepository stayRepository,
        Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService regionReferenceService)
    {
        _stayRepository = stayRepository;
        _regionReferenceService = regionReferenceService;
    }

    public async Task<List<StaySummaryDto>> HandleAsync(
        GetAllStaysQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.MinPrice.HasValue && query.MinPrice.Value < 0)
            throw new ArgumentException("MinPrice cannot be negative.");

        if (query.MaxPrice.HasValue && query.MaxPrice.Value < 0)
            throw new ArgumentException("MaxPrice cannot be negative.");

        if (query.MinPrice.HasValue &&
            query.MaxPrice.HasValue &&
            query.MinPrice.Value > query.MaxPrice.Value)
            throw new ArgumentException("MinPrice cannot be greater than MaxPrice.");

        if (query.Guests.HasValue && query.Guests.Value <= 0)
            throw new ArgumentException("Guests must be greater than 0.");

        var stays = await _stayRepository.GetFilteredAsync(
            query.Adm3Gid,
            query.MinPrice,
            query.MaxPrice,
            query.Guests,
            query.Tag,
            cancellationToken);

        var regions = await _regionReferenceService.GetNeighbourhoodsAsync(
            stays.Select(x => x.Adm3Gid),
            cancellationToken);

        return stays
            .Select(stay => Glinter.Modules.Stays.Application.Common.Mapping.StayMappings
                .ToSummaryDto(stay, regions.GetValueOrDefault(stay.Adm3Gid)))
            .ToList();
    }
}
