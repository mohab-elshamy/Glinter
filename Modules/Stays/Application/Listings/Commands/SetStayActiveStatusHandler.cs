using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;

namespace Glinter.Modules.Stays.Application.Listings.Commands;

public class SetStayActiveStatusHandler
{
    private readonly IStayRepository _stayRepository;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;

    public SetStayActiveStatusHandler(
        IStayRepository stayRepository,
        Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService regionReferenceService)
    {
        _stayRepository = stayRepository;
        _regionReferenceService = regionReferenceService;
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

        var region = await _regionReferenceService.GetNeighbourhoodAsync(
            updatedStay.Adm3Gid,
            cancellationToken);

        return Glinter.Modules.Stays.Application.Common.Mapping.StayMappings
            .ToResponseDto(updatedStay, region);
    }
}
