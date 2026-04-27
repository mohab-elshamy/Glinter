using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;

namespace Glinter.Modules.Stays.Application.Listings.Commands;

public class SetStayActiveStatusHandler
{
    private readonly IStayRepository _stayRepository;

    public SetStayActiveStatusHandler(IStayRepository stayRepository)
    {
        _stayRepository = stayRepository;
    }

    public async Task<StayResponseDto?> HandleAsync(
        SetStayActiveStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var stay = await _stayRepository.GetForUpdateAsync(command.StayId, cancellationToken);

        if (stay is null)
            return null;

        stay.IsActive = command.IsActive;
        stay.UpdatedAtUtc = DateTime.UtcNow;

        await _stayRepository.UpdateAsync(stay, cancellationToken);

        var updatedStay = await _stayRepository.GetByIdAsync(stay.Id, cancellationToken);

        if (updatedStay is null)
            return null;

        return new StayResponseDto
        {
            Id = updatedStay.Id,
            OwnerProfileId = updatedStay.OwnerProfileId,
            AreaId = updatedStay.AreaId,
            Name = updatedStay.Name,
            Description = updatedStay.Description,
            Address = updatedStay.Address,
            PricePerNight = updatedStay.PricePerNight,
            Currency = updatedStay.Currency,
            MaxGuests = updatedStay.MaxGuests,
            Latitude = updatedStay.Latitude,
            Longitude = updatedStay.Longitude,
            IsActive = updatedStay.IsActive,
            CreatedAtUtc = updatedStay.CreatedAtUtc,
            UpdatedAtUtc = updatedStay.UpdatedAtUtc,
            Tags = updatedStay.Tags.Select(t => new StayTagDto
            {
                Id = t.Id,
                StayId = t.StayId,
                Name = t.Name
            }).ToList()
        };
    }
}