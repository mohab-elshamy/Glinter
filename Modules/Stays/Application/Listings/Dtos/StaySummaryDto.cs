namespace Glinter.Modules.Stays.Application.Listings.Dtos;

public class StaySummaryDto
{
    public Guid Id { get; set; }
    public int Adm3Gid { get; set; }
    public Glinter.Modules.Regions.Application.DTOs.RegionReferenceDto? Region { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public string Currency { get; set; } = "EGP";
    public int MaxGuests { get; set; }
    public bool IsActive { get; set; }
}
