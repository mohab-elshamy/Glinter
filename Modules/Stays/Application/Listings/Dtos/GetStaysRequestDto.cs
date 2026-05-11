namespace Glinter.Modules.Stays.Application.Listings.Dtos;

public class GetStaysRequestDto
{
    public Guid? AreaId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? Guests { get; set; }
    public string? Tag { get; set; }
}