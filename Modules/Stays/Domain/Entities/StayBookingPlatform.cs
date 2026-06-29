namespace Glinter.Modules.Stays.Domain.Entities;

public class StayBookingPlatform
{
    public int Id { get; set; }
    public int StayId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? PriceWithTax { get; set; }
    public string? Link { get; set; }

    public Stay Stay { get; set; } = null!;
}
