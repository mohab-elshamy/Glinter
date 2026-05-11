using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Dtos;
using Glinter.Modules.Stays.Domain.Entities;

namespace Glinter.Modules.Stays.Application.Listings.Commands;

public class CreateStayHandler
{
    private readonly IStayRepository _stayRepository;

    public CreateStayHandler(IStayRepository stayRepository)
    {
        _stayRepository = stayRepository;
    }

    public async Task<StayResponseDto> HandleAsync(CreateStayCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Stay name is required.");

        if (command.PricePerNight <= 0)
            throw new ArgumentException("PricePerNight must be greater than 0.");

        if (command.MaxGuests <= 0)
            throw new ArgumentException("MaxGuests must be greater than 0.");

        var normalizedName = command.Name.Trim();
        var normalizedAddress = command.Address?.Trim() ?? string.Empty;

        var alreadyExists = await _stayRepository.ExistsAsync(
            command.OwnerProfileId,
            normalizedName,
            normalizedAddress,
            cancellationToken);

        if (alreadyExists)
            throw new ArgumentException("A stay with the same owner, name, and address already exists.");

        var stay = new Stay
        {
            Id = Guid.NewGuid(),
            OwnerProfileId = command.OwnerProfileId,
            AreaId = command.AreaId,
            Name = normalizedName,
            Description = command.Description?.Trim() ?? string.Empty,
            Address = normalizedAddress,
            PricePerNight = command.PricePerNight,
            Currency = string.IsNullOrWhiteSpace(command.Currency) ? "EGP" : command.Currency.Trim(),
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