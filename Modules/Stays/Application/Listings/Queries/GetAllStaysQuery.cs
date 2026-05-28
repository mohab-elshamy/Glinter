namespace Glinter.Modules.Stays.Application.Listings.Queries;

public class GetAllStaysQuery
{
    public Guid? AreaId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? Guests { get; set; }
    public string? Tag { get; set; }
}