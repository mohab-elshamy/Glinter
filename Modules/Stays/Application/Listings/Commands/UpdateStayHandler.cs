using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;

namespace Glinter.Modules.Stays.Application.Listings.Commands;

public class UpdateStayHandler
{
    private readonly IStayRepository _stayRepository;

    public UpdateStayHandler(IStayRepository stayRepository)
    {
        _stayRepository = stayRepository;
    }

    public async Task<StayResponseDto?> HandleAsync(UpdateStayCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Stay name is required.");

        if (command.PricePerNight <= 0)
            throw new ArgumentException("PricePerNight must be greater than 0.");

        if (command.MaxGuests <= 0)
            throw new ArgumentException("MaxGuests must be greater than 0.");

        var stay = await _stayRepository.GetForUpdateAsync(command.Id, cancellationToken);

        if (stay is null)
            return null;

        stay.Name = command.Name.Trim();
        stay.Description = command.Description?.Trim() ?? string.Empty;
        stay.Address = command.Address?.Trim() ?? string.Empty;
        stay.PricePerNight = command.PricePerNight;
        stay.Currency = string.IsNullOrWhiteSpace(command.Currency) ? "EGP" : command.Currency.Trim();
        stay.MaxGuests = command.MaxGuests;
        stay.Latitude = command.Latitude;
        stay.Longitude = command.Longitude;
        stay.UpdatedAtUtc = DateTime.UtcNow;

        await _stayRepository.ReplaceTagsAsync(
            stay.Id,
            command.Tags ?? Enumerable.Empty<string>(),
            cancellationToken);

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