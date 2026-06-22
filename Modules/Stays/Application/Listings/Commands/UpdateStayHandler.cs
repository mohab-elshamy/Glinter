using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;

namespace Glinter.Modules.Stays.Application.Listings.Commands;

public class UpdateStayHandler
{
    private readonly IStayRepository _stayRepository;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;

    public UpdateStayHandler(
        IStayRepository stayRepository,
        Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService regionReferenceService)
    {
        _stayRepository = stayRepository;
        _regionReferenceService = regionReferenceService;
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

        var region = await _regionReferenceService.GetNeighbourhoodAsync(
            updatedStay.Adm3Gid,
            cancellationToken);

        return Glinter.Modules.Stays.Application.Common.Mapping.StayMappings
            .ToResponseDto(updatedStay, region);
    }
}
