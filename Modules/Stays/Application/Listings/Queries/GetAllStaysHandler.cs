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
        if (query.Page < 1 || query.Page > GetAllStaysQuery.MaxPage)
            throw new ValidationException($"Page must be between 1 and {GetAllStaysQuery.MaxPage}.");

        if (query.PageSize < 1 || query.PageSize > GetAllStaysQuery.MaxPageSize)
            throw new ValidationException($"PageSize must be between 1 and {GetAllStaysQuery.MaxPageSize}.");

        if (query.MinPrice.HasValue && query.MinPrice.Value < 0)
            throw new ValidationException("MinPrice cannot be negative.");

        if (query.MaxPrice.HasValue && query.MaxPrice.Value < 0)
            throw new ValidationException("MaxPrice cannot be negative.");

        if (query.MinPrice.HasValue &&
            query.MaxPrice.HasValue &&
            query.MinPrice.Value > query.MaxPrice.Value)
            throw new ValidationException("MinPrice cannot be greater than MaxPrice.");

        if (query.Guests.HasValue && query.Guests.Value <= 0)
            throw new ValidationException("Guests must be greater than 0.");

        if (query.Adm3Gid.HasValue && query.Adm3Gid.Value <= 0)
            throw new ValidationException("Adm3Gid must be greater than 0.");

        if (!string.IsNullOrWhiteSpace(query.Tag) && query.Tag.Trim().Length > 100)
            throw new ValidationException("Tag cannot exceed 100 characters.");

        var stays = await _stayRepository.GetFilteredAsync(
            query.Adm3Gid,
            query.MinPrice,
            query.MaxPrice,
            query.Guests,
            query.Tag,
            query.Page,
            query.PageSize,
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
