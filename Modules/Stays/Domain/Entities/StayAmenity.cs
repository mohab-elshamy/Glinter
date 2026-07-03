namespace Glinter.Modules.Stays.Domain.Entities;

public class StayAmenity
{
    public int Id { get; set; }
    public int StayId { get; set; }
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }

    public Stay Stay { get; set; } = null!;
}
