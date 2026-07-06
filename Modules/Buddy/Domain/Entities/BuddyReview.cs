namespace Glinter.Modules.Buddy.Domain.Entities;

public sealed class BuddyReview
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid LocalBuddyUserId { get; set; }
    public Guid TravelerUserId { get; set; }
    public int Rating { get; set; }
    public string ReviewText { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public BuddyBooking Booking { get; set; } = null!;
}
