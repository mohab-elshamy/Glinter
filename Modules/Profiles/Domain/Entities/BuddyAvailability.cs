namespace Glinter.Modules.Profiles.Domain.Entities;

public sealed class BuddyAvailability
{
    public Guid Id { get; set; }
    public Guid LocalBuddyUserId { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<BuddyBooking> Bookings { get; set; } = [];
}
