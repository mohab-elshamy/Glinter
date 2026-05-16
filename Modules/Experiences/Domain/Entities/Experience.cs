namespace Glinter.Modules.Experiences.Domain.Entities;

public class Experience
{
    public Guid Id { get; set; }

    public Guid ProviderProfileId { get; set; }

    public Guid CategoryId { get; set; }

    public Guid AreaId { get; set; }

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

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ExperienceCategory? Category { get; set; }

    public ICollection<ExperienceAvailability> AvailabilitySlots { get; set; } = new List<ExperienceAvailability>();

    public ICollection<ExperienceBooking> Bookings { get; set; } = new List<ExperienceBooking>();

    public ICollection<ExperienceReview> Reviews { get; set; } = new List<ExperienceReview>();

    public ICollection<ExperienceVibe> ExperienceVibes { get; set; } = new List<ExperienceVibe>();

    public ICollection<ExperienceTag> Tags { get; set; } = new List<ExperienceTag>();
}