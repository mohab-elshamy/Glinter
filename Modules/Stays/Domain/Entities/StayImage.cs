namespace Glinter.Modules.Stays.Domain.Entities;

public class StayImage
{
    public int Id { get; set; }
    public int StayId { get; set; }
    public string Link { get; set; } = string.Empty;

    public Stay Stay { get; set; } = null!;
}
