using Glinter.Modules.Stays.Domain.Enums;

namespace Glinter.Modules.Stays.Domain.Entities;

public class StayBooking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int StayId { get; set; }
    public Stay Stay { get; set; } = null!;
    public Guid TravelerProfileId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int GuestCount { get; set; }
    public decimal TotalPrice { get; set; }
    public StayBookingStatus Status { get; set; } = StayBookingStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public StayReview? Review { get; set; }
}
