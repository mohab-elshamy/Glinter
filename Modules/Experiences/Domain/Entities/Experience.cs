namespace Glinter.Modules.Experiences.Domain.Entities;

using Glinter.Modules.Experiences.Domain.Enums;

public class Experience
{
    public Guid Id { get; set; }

    public Guid ProviderProfileId { get; set; }

    public Guid CategoryId { get; set; }

    public int Adm3Gid { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public decimal PricePerPerson { get; set; }

    public string Currency { get; set; } = "EGP";

    public int DurationMinutes { get; set; }

    public int MaxGuests { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public bool IsActive { get; set; } = true;

    public ExperienceApprovalStatus ApprovalStatus { get; set; } = ExperienceApprovalStatus.Pending;

    public string? ModerationNotes { get; set; }

    public DateTime? ModeratedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ExperienceCategory? Category { get; set; }

    public ICollection<ExperienceAvailability> AvailabilitySlots { get; set; } = new List<ExperienceAvailability>();

    public ICollection<ExperienceBooking> Bookings { get; set; } = new List<ExperienceBooking>();

    public ICollection<ExperienceReview> Reviews { get; set; } = new List<ExperienceReview>();

    public ICollection<ExperienceVibe> ExperienceVibes { get; set; } = new List<ExperienceVibe>();

    public ICollection<ExperienceTag> Tags { get; set; } = new List<ExperienceTag>();

    public ICollection<ExperienceModerationEvent> ModerationHistory { get; set; } =
        new List<ExperienceModerationEvent>();
}
