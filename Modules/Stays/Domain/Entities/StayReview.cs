namespace Glinter.Modules.Stays.Domain.Entities;

public class StayReview
{
    public Guid Id { get; set; }

    public Guid StayId { get; set; }
    public Stay Stay { get; set; } = null!;

    public Guid TravelerProfileId { get; set; }

    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}