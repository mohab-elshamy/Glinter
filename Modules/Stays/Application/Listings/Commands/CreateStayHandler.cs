using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;
using Glinter.Modules.Stays.Domain.Entities;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.LocationCatalog.Application.Abstractions;

namespace Glinter.Modules.Stays.Application.Listings.Commands;

public class CreateStayHandler
{
    private readonly IStayRepository _stayRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProfilesReadService _profilesReadService;
    private readonly ILocationCatalogReadService _locationCatalogReadService;
    
    public CreateStayHandler(
        IStayRepository stayRepository,
        ICurrentUserService currentUserService,
        IProfilesReadService profilesReadService,
        ILocationCatalogReadService locationCatalogReadService)
    {
        _stayRepository = stayRepository;
        _currentUserService = currentUserService;
        _profilesReadService = profilesReadService;
        _locationCatalogReadService = locationCatalogReadService;
    }

    public async Task<StayResponseDto> HandleAsync(CreateStayCommand command, CancellationToken cancellationToken = default)
    {
        
        var areaExists = await _locationCatalogReadService.AreaExistsAsync(
            command.AreaId,
            cancellationToken);

        if (!areaExists)
        {
            throw new InvalidOperationException("Invalid areaId. The selected area does not exist.");
        }
        
        
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var currentUserId = _currentUserService.UserId.Value;

        var ownerProfileId = await _profilesReadService
            .GetHotelOwnerProfileIdByUserIdAsync(currentUserId, cancellationToken);

        if (ownerProfileId is null)
        {
            throw new UnauthorizedAccessException("Hotel owner profile is required before creating stays.");
        }
        
        
        
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Stay name is required.");

        if (command.PricePerNight <= 0)
            throw new ArgumentException("PricePerNight must be greater than 0.");

        if (command.MaxGuests <= 0)
            throw new ArgumentException("MaxGuests must be greater than 0.");

        var normalizedName = command.Name.Trim();
        var normalizedAddress = command.Address?.Trim() ?? string.Empty;

        var alreadyExists = await _stayRepository.ExistsAsync(
            ownerProfileId.Value,
            normalizedName,
            normalizedAddress,
            cancellationToken);

        if (alreadyExists)
            throw new ArgumentException("A stay with the same owner, name, and address already exists.");

        var stay = new Stay
        {
            Id = Guid.NewGuid(),
            OwnerProfileId = ownerProfileId.Value,
            AreaId = command.AreaId,
            Name = command.Name,
            Description = command.Description ?? string.Empty,
            Address = command.Address ?? string.Empty,
            PricePerNight = command.PricePerNight,
            Currency = command.Currency ?? "EGP",
            MaxGuests = command.MaxGuests,
            Latitude = command.Latitude,
            Longitude = command.Longitude,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        if (command.Tags is not null && command.Tags.Count > 0)
        {
            stay.Tags = command.Tags
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => new StayTag
                {
                    Id = Guid.NewGuid(),
                    StayId = stay.Id,
                    Name = x.Trim()
                })
                .ToList();
        }

        var createdStay = await _stayRepository.AddAsync(stay, cancellationToken);

        return new StayResponseDto
        {
            Id = createdStay.Id,
            OwnerProfileId = createdStay.OwnerProfileId,
            AreaId = createdStay.AreaId,
            Name = createdStay.Name,
            Description = createdStay.Description,
            Address = createdStay.Address,
            PricePerNight = createdStay.PricePerNight,
            Currency = createdStay.Currency,
            MaxGuests = createdStay.MaxGuests,
            Latitude = createdStay.Latitude,
            Longitude = createdStay.Longitude,
            IsActive = createdStay.IsActive,
            CreatedAtUtc = createdStay.CreatedAtUtc,
            UpdatedAtUtc = createdStay.UpdatedAtUtc,
            Tags = createdStay.Tags.Select(t => new StayTagDto
            {
                Id = t.Id,
                StayId = t.StayId,
                Name = t.Name
            }).ToList()
        };
    }
}