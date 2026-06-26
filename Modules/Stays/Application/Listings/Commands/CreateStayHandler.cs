using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;
using Glinter.Modules.Stays.Domain.Entities;

namespace Glinter.Modules.Stays.Application.Listings.Commands;

public class CreateStayHandler
{
    private const decimal MaxDatabaseMoneyValue = 9_999_999_999_999_999.99m;
    private const int MaxTags = 50;

    private readonly IStayRepository _stayRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProfilesReadService _profilesReadService;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;

    public CreateStayHandler(
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

    public async Task<StayResponseDto> HandleAsync(CreateStayCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command);

        var ownerProfileId = await GetCurrentOwnerProfileIdAsync(cancellationToken);

        var region = await _regionReferenceService.GetNeighbourhoodAsync(
            command.Adm3Gid,
            cancellationToken);

        if (region is null)
            throw new ArgumentException("Adm3Gid must reference an existing neighbourhood.");

        var normalizedName = command.Name.Trim();
        var normalizedAddress = command.Address?.Trim() ?? string.Empty;

        var alreadyExists = await _stayRepository.ExistsAsync(
            ownerProfileId,
            normalizedName,
            normalizedAddress,
            cancellationToken);

        if (alreadyExists)
            throw new ArgumentException("A stay with the same owner, name, and address already exists.");

        var stay = new Stay
        {
            Id = Guid.NewGuid(),
            OwnerProfileId = ownerProfileId,
            Adm3Gid = command.Adm3Gid,
            Name = normalizedName,
            Description = command.Description?.Trim() ?? string.Empty,
            Address = normalizedAddress,
            PricePerNight = command.PricePerNight,
            Currency = command.Currency.Trim().ToUpper(),
            MaxGuests = command.MaxGuests,
            Latitude = command.Latitude,
            Longitude = command.Longitude,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        stay.Tags = NormalizeTags(command.Tags)
            .Select(tag => new StayTag
            {
                Id = Guid.NewGuid(),
                StayId = stay.Id,
                Name = tag
            })
            .ToList();

        var createdStay = await _stayRepository.AddAsync(stay, cancellationToken);

        return Glinter.Modules.Stays.Application.Common.Mapping.StayMappings
            .ToResponseDto(createdStay, region);
    }

    private async Task<Guid> GetCurrentOwnerProfileIdAsync(CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var ownerProfileId = await _profilesReadService.GetHotelOwnerProfileIdByUserIdAsync(
            _currentUserService.UserId.Value,
            cancellationToken);

        return ownerProfileId
               ?? throw new UnauthorizedAccessException("Only hotel owners can create stays.");
    }

    private static void Validate(CreateStayCommand command)
    {
        if (command.Adm3Gid <= 0)
            throw new ArgumentException("Adm3Gid must be greater than 0.");

        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Stay name is required.");

        if (command.Name.Trim().Length > 200)
            throw new ArgumentException("Stay name cannot exceed 200 characters.");

        if ((command.Description?.Trim().Length ?? 0) > 2000)
            throw new ArgumentException("Description cannot exceed 2000 characters.");

        if (string.IsNullOrWhiteSpace(command.Address))
            throw new ArgumentException("Address is required.");

        if (command.Address.Trim().Length > 500)
            throw new ArgumentException("Address cannot exceed 500 characters.");

        if (command.PricePerNight <= 0)
            throw new ArgumentException("PricePerNight must be greater than 0.");

        if (command.PricePerNight > MaxDatabaseMoneyValue)
            throw new ArgumentException("PricePerNight exceeds the maximum supported value.");

        if (string.IsNullOrWhiteSpace(command.Currency))
            throw new ArgumentException("Currency is required.");

        if (command.Currency.Trim().Length > 10)
            throw new ArgumentException("Currency cannot exceed 10 characters.");

        if (command.MaxGuests <= 0)
            throw new ArgumentException("MaxGuests must be greater than 0.");

        if (double.IsNaN(command.Latitude) || double.IsInfinity(command.Latitude) ||
            command.Latitude < -90 || command.Latitude > 90)
            throw new ArgumentException("Latitude must be between -90 and 90.");

        if (double.IsNaN(command.Longitude) || double.IsInfinity(command.Longitude) ||
            command.Longitude < -180 || command.Longitude > 180)
            throw new ArgumentException("Longitude must be between -180 and 180.");

        if (command.Tags is not null && command.Tags.Any(tag => !string.IsNullOrWhiteSpace(tag) && tag.Trim().Length > 100))
            throw new ArgumentException("Tags cannot exceed 100 characters.");

        if (command.Tags is { Count: > MaxTags })
            throw new ArgumentException($"A stay cannot have more than {MaxTags} tags.");
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
