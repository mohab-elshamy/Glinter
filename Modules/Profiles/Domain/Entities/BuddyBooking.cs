using Glinter.Modules.Profiles.Domain.Enums;

namespace Glinter.Modules.Profiles.Domain.Entities;

public sealed class BuddyBooking
{
    public Guid Id { get; set; }
    public Guid AvailabilityId { get; set; }
    public Guid LocalBuddyUserId { get; set; }
    public Guid TravelerUserId { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public BuddyBookingStatus Status { get; set; } = BuddyBookingStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public BuddyAvailability Availability { get; set; } = null!;
    public BuddyReview? Review { get; set; }
}
