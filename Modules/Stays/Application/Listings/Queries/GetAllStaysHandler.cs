using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;

namespace Glinter.Modules.Stays.Application.Listings.Queries;

public class GetAllStaysHandler
{
    private readonly IStayRepository _stayRepository;

    public GetAllStaysHandler(IStayRepository stayRepository)
    {
        _stayRepository = stayRepository;
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
            query.AreaId,
            query.MinPrice,
            query.MaxPrice,
            query.Guests,
            query.Tag,
            cancellationToken);

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