using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;

namespace Glinter.Modules.Stays.Application.Listings.Queries;

public class GetStayByIdHandler
{
    private readonly IStayRepository _stayRepository;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;

    public GetStayByIdHandler(
        IStayRepository stayRepository,
        Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService regionReferenceService)
    {
        _stayRepository = stayRepository;
        _regionReferenceService = regionReferenceService;
    }

    public async Task<StayResponseDto?> HandleAsync(GetStayByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Id == Guid.Empty)
            throw new ArgumentException("Stay id is required.");

        var stay = await _stayRepository.GetByIdAsync(query.Id, cancellationToken);

        if (stay is null || !stay.IsActive)
            return null;

        var region = await _regionReferenceService.GetNeighbourhoodAsync(
            stay.Adm3Gid,
            cancellationToken);

        return Glinter.Modules.Stays.Application.Common.Mapping.StayMappings
            .ToResponseDto(stay, region);
    }
}
