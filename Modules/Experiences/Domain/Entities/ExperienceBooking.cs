using Glinter.Modules.Experiences.Domain.Enums;

namespace Glinter.Modules.Experiences.Domain.Entities;

public class ExperienceBooking
{
    public Guid Id { get; set; }

    public Guid ExperienceId { get; set; }

    public Guid AvailabilityId { get; set; }

    public Guid TravelerProfileId { get; set; }

    public int GuestsCount { get; set; }

    public decimal TotalPrice { get; set; }

    public ExperienceBookingStatus Status { get; set; } = ExperienceBookingStatus.Confirmed;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CancelledAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public Experience? Experience { get; set; }

    public ExperienceAvailability? Availability { get; set; }
}