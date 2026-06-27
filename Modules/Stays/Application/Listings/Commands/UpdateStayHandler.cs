using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;

namespace Glinter.Modules.Stays.Application.Listings.Commands;

public class UpdateStayHandler
{
    private const decimal MaxDatabaseMoneyValue = 9_999_999_999_999_999.99m;
    private const int MaxTags = 50;

    private readonly IStayRepository _stayRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProfilesReadService _profilesReadService;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;

    public UpdateStayHandler(
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

    public async Task<StayResponseDto?> HandleAsync(UpdateStayCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command);

        var ownerProfileId = await GetCurrentOwnerProfileIdAsync(cancellationToken);

        var stay = await _stayRepository.GetForUpdateAsync(command.Id, cancellationToken);

        if (stay is null)
            return null;

        if (stay.OwnerProfileId != ownerProfileId)
            throw new ForbiddenException("You can update only your own stays.");

        var normalizedName = command.Name.Trim();
        var normalizedAddress = command.Address?.Trim() ?? string.Empty;

        var alreadyExists = await _stayRepository.ExistsAsync(
            ownerProfileId,
            normalizedName,
            normalizedAddress,
            stay.Id,
            cancellationToken);

        if (alreadyExists)
            throw new ValidationException("A stay with the same owner, name, and address already exists.");

        stay.Name = normalizedName;
        stay.Description = command.Description?.Trim() ?? string.Empty;
        stay.Address = normalizedAddress;
        stay.PricePerNight = command.PricePerNight;
        stay.Currency = command.Currency.Trim().ToUpper();
        stay.MaxGuests = command.MaxGuests;
        stay.Latitude = command.Latitude;
        stay.Longitude = command.Longitude;
        stay.UpdatedAtUtc = DateTime.UtcNow;

        await _stayRepository.ReplaceTagsAsync(
            stay.Id,
            NormalizeTags(command.Tags),
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

    private async Task<Guid> GetCurrentOwnerProfileIdAsync(CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");

        var ownerProfileId = await _profilesReadService.GetHotelOwnerProfileIdByUserIdAsync(
            _currentUserService.UserId.Value,
            cancellationToken);

        return ownerProfileId
               ?? throw new ForbiddenException("Only hotel owners can update stays.");
    }

    private static void Validate(UpdateStayCommand command)
    {
        if (command.Id == Guid.Empty)
            throw new ValidationException("Stay id is required.");

        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ValidationException("Stay name is required.");

        if (command.Name.Trim().Length > 200)
            throw new ValidationException("Stay name cannot exceed 200 characters.");

        if ((command.Description?.Trim().Length ?? 0) > 2000)
            throw new ValidationException("Description cannot exceed 2000 characters.");

        if (string.IsNullOrWhiteSpace(command.Address))
            throw new ValidationException("Address is required.");

        if (command.Address.Trim().Length > 500)
            throw new ValidationException("Address cannot exceed 500 characters.");

        if (command.PricePerNight <= 0)
            throw new ValidationException("PricePerNight must be greater than 0.");

        if (command.PricePerNight > MaxDatabaseMoneyValue)
            throw new ValidationException("PricePerNight exceeds the maximum supported value.");

        if (string.IsNullOrWhiteSpace(command.Currency))
            throw new ValidationException("Currency is required.");

        if (command.Currency.Trim().Length > 10)
            throw new ValidationException("Currency cannot exceed 10 characters.");

        if (command.MaxGuests <= 0)
            throw new ValidationException("MaxGuests must be greater than 0.");

        if (double.IsNaN(command.Latitude) || double.IsInfinity(command.Latitude) ||
            command.Latitude < -90 || command.Latitude > 90)
            throw new ValidationException("Latitude must be between -90 and 90.");

        if (double.IsNaN(command.Longitude) || double.IsInfinity(command.Longitude) ||
            command.Longitude < -180 || command.Longitude > 180)
            throw new ValidationException("Longitude must be between -180 and 180.");

        if (command.Tags is not null && command.Tags.Any(tag => !string.IsNullOrWhiteSpace(tag) && tag.Trim().Length > 100))
            throw new ValidationException("Tags cannot exceed 100 characters.");

        if (command.Tags is { Count: > MaxTags })
            throw new ValidationException($"A stay cannot have more than {MaxTags} tags.");
    }

    private static IEnumerable<string> NormalizeTags(IEnumerable<string>? tags)
    {
        return tags?
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            ?? Enumerable.Empty<string>();
    }
}
