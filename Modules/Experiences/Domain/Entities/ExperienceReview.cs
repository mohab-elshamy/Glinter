namespace Glinter.Modules.Experiences.Domain.Entities;

public class ExperienceReview
{
    public int Id { get; set; }
    public int ExperienceId { get; set; }
    public Guid? BookingId { get; set; }
    public string? ExternalReviewId { get; set; }
    public string? ReviewerName { get; set; }
    public int? Rating { get; set; }
    public string? ReviewText { get; set; }
    public DateTime? PublishedAtDate { get; set; }
    public string SourceList { get; set; } = "featured_reviews";
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Experience Experience { get; set; } = null!;
    public ExperienceBooking? Booking { get; set; }
}
