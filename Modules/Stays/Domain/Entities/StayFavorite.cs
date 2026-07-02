namespace Glinter.Modules.Stays.Domain.Entities;

public sealed class StayFavorite
{
    public Guid UserId { get; set; }
    public int StayId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Stay Stay { get; set; } = null!;
}
