using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;

namespace Glinter.Modules.Stays.Application.Listings.Commands;

public class SetStayActiveStatusHandler
{
    private readonly IStayRepository _stayRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProfilesReadService _profilesReadService;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;

    public SetStayActiveStatusHandler(
        IStayRepository stayRepository,
        ICurrentUserService currentUserService,
        IProfilesReadService profilesReadService,
        Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService regionReferenceService)
    {
        _stayRepository = stayRepository;
        _currentUserService = currentUserService;
        _profilesReadService = profilesReadService;
        _regionReferenceService = regionReferenceService;
    }

    public async Task<StayResponseDto?> HandleAsync(
        SetStayActiveStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.StayId == Guid.Empty)
            throw new ValidationException("Stay id is required.");

        var ownerProfileId = await GetCurrentOwnerProfileIdAsync(cancellationToken);

        var stay = await _stayRepository.GetForUpdateAsync(command.StayId, cancellationToken);

        if (stay is null)
            return null;

        if (stay.OwnerProfileId != ownerProfileId)
            throw new ForbiddenException("You can change status only for your own stays.");

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

    private async Task<Guid> GetCurrentOwnerProfileIdAsync(CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");

        var ownerProfileId = await _profilesReadService.GetHotelOwnerProfileIdByUserIdAsync(
            _currentUserService.UserId.Value,
            cancellationToken);

        return ownerProfileId
               ?? throw new ForbiddenException("Only hotel owners can manage stays.");
    }
}
