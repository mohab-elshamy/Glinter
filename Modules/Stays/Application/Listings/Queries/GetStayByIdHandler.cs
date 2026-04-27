using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;

namespace Glinter.Modules.Stays.Application.Listings.Queries;

public class GetStayByIdHandler
{
    private readonly IStayRepository _stayRepository;

    public GetStayByIdHandler(IStayRepository stayRepository)
    {
        _stayRepository = stayRepository;
    }

    public async Task<StayResponseDto?> HandleAsync(GetStayByIdQuery query, CancellationToken cancellationToken = default)
    {
        var stay = await _stayRepository.GetByIdAsync(query.Id, cancellationToken);

        if (stay is null)
            return null;

        return new StayResponseDto
        {
            Id = stay.Id,
            OwnerProfileId = stay.OwnerProfileId,
            AreaId = stay.AreaId,
            Name = stay.Name,
            Description = stay.Description,
            Address = stay.Address,
            PricePerNight = stay.PricePerNight,
            Currency = stay.Currency,
            MaxGuests = stay.MaxGuests,
            Latitude = stay.Latitude,
            Longitude = stay.Longitude,
            IsActive = stay.IsActive,
            CreatedAtUtc = stay.CreatedAtUtc,
            UpdatedAtUtc = stay.UpdatedAtUtc,
            Tags = stay.Tags.Select(t => new StayTagDto
            {
                Id = t.Id,
                StayId = t.StayId,
                Name = t.Name
            }).ToList()
        };
    }
}