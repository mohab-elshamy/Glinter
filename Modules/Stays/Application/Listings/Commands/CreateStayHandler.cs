using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;
using Glinter.Modules.Stays.Domain.Entities;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;

namespace Glinter.Modules.Stays.Application.Listings.Commands;

public class CreateStayHandler
{
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




        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var currentUserId = _currentUserService.UserId.Value;

        var ownerProfileId = await _profilesReadService
            .GetHotelOwnerProfileIdByUserIdAsync(currentUserId, cancellationToken);

        if (ownerProfileId is null)
        {
            throw new UnauthorizedAccessException("Only hotel owners can create stays.");
        }



        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Stay name is required.");

        if (command.PricePerNight <= 0)
            throw new ArgumentException("PricePerNight must be greater than 0.");

        if (command.MaxGuests <= 0)
            throw new ArgumentException("MaxGuests must be greater than 0.");

        var region = await _regionReferenceService.GetNeighbourhoodAsync(
            command.Adm3Gid,
            cancellationToken);

        if (region is null)
            throw new ArgumentException("Adm3Gid must reference an existing neighbourhood.");

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
            Adm3Gid = command.Adm3Gid,
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

        return Glinter.Modules.Stays.Application.Common.Mapping.StayMappings
            .ToResponseDto(createdStay, region);
    }
}
