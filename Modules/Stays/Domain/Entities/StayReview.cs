namespace Glinter.Modules.Stays.Domain.Entities;

public class StayReview
{
    public int Id { get; set; }
    public int StayId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? ExternalReviewId { get; set; }
    public string? ReviewerName { get; set; }
    public int? Rating { get; set; }
    public string? ReviewText { get; set; }
    public string Platform { get; set; } = "glinter";
    public DateTime? PublishedAtDate { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Stay Stay { get; set; } = null!;
    public StayBooking? Booking { get; set; }
}
