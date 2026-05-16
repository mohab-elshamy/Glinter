namespace Glinter.Modules.Experiences.Domain.Entities;

public class ExperienceAvailability
{
    public Guid Id { get; set; }

    public Guid ExperienceId { get; set; }

    public DateTime StartTimeUtc { get; set; }

    public DateTime EndTimeUtc { get; set; }

    public int Capacity { get; set; }

    public int BookedCount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Experience? Experience { get; set; }

    public ICollection<ExperienceBooking> Bookings { get; set; } = new List<ExperienceBooking>();
}