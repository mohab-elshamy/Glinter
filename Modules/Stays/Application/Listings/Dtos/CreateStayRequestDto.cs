namespace Glinter.Modules.Stays.Application.Listings.Dtos;

public class CreateStayRequestDto
{
    public Guid OwnerProfileId { get; set; }
    public Guid AreaId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }

    public decimal PricePerNight { get; set; }
    public string Currency { get; set; } = "EGP";
    public int MaxGuests { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public List<string>? Tags { get; set; }
}