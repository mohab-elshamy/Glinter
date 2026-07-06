using Glinter.Modules.Experiences.Domain.Enums;

namespace Glinter.Modules.Experiences.Domain.Entities;

public class ExperienceBooking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ExperienceId { get; set; }
    public Experience Experience { get; set; } = null!;
    public Guid AvailabilityId { get; set; }
    public ExperienceAvailability Availability { get; set; } = null!;
    public Guid TravelerProfileId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string TravelerName { get; set; } = string.Empty;
    public int GuestsCount { get; set; }
    public decimal TotalPrice { get; set; }
    public ExperienceBookingStatus Status { get; set; } = ExperienceBookingStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ExperienceReview? Review { get; set; }
}
