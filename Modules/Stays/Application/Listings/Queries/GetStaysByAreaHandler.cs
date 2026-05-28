using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;

namespace Glinter.Modules.Stays.Application.Listings.Queries;

public class GetStaysByAreaHandler
{
    private readonly IStayRepository _stayRepository;

    public GetStaysByAreaHandler(IStayRepository stayRepository)
    {
        _stayRepository = stayRepository;
    }

    public async Task<List<StaySummaryDto>> HandleAsync(
        GetStaysByAreaQuery query,
        CancellationToken cancellationToken = default)
    {
        var stays = await _stayRepository.GetByAreaIdAsync(query.AreaId, cancellationToken);

        return stays.Select(stay => new StaySummaryDto
        {
            Id = stay.Id,
            Name = stay.Name,
            Address = stay.Address,
            PricePerNight = stay.PricePerNight,
            Currency = stay.Currency,
            MaxGuests = stay.MaxGuests,
            IsActive = stay.IsActive
        }).ToList();
    }
}