namespace Glinter.Modules.Stays.Domain.Entities;

public class StayAmenity
{
    public int Id { get; set; }
    public int StayId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Stay Stay { get; set; } = null!;
}
