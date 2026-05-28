namespace Glinter.Modules.Stays.Application.Listings.Dtos;

public class StayResponseDto
{
    public Guid Id { get; set; }
    public Guid OwnerProfileId { get; set; }
    public Guid AreaId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public decimal PricePerNight { get; set; }
    public string Currency { get; set; } = "EGP";
    public int MaxGuests { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public List<StayTagDto> Tags { get; set; } = new();
}