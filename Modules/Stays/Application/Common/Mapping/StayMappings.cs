using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Stays.Application.Listings.Dtos;
using Glinter.Modules.Stays.Domain.Entities;

namespace Glinter.Modules.Stays.Application.Common.Mapping;

public static class StayMappings
{
    public static StayResponseDto ToResponseDto(
        this Stay stay,
        RegionReferenceDto? region)
    {
        return new StayResponseDto
        {
            Id = stay.Id,
            OwnerProfileId = stay.OwnerProfileId,
            Adm3Gid = stay.Adm3Gid,
            Region = region,
            Name = stay.Name,
            Description = stay.Description,
            Address = stay.Address,
            PricePerNight = stay.PricePerNight,
            Currency = stay.Currency,
            MaxGuests = stay.MaxGuests,
            Latitude = stay.Latitude,
            Longitude = stay.Longitude,
            IsActive = stay.IsActive,
            CreatedAtUtc = stay.CreatedAtUtc,
            UpdatedAtUtc = stay.UpdatedAtUtc,
            Tags = stay.Tags.Select(t => new StayTagDto
            {
                Id = t.Id,
                StayId = t.StayId,
                Name = t.Name
            }).ToList()
        };
    }

    public static StaySummaryDto ToSummaryDto(
        this Stay stay,
        RegionReferenceDto? region)
    {
        return new StaySummaryDto
        {
            Id = stay.Id,
            Adm3Gid = stay.Adm3Gid,
            Region = region,
            Name = stay.Name,
            Address = stay.Address,
            PricePerNight = stay.PricePerNight,
            Currency = stay.Currency,
            MaxGuests = stay.MaxGuests,
            IsActive = stay.IsActive
        };
    }
}
