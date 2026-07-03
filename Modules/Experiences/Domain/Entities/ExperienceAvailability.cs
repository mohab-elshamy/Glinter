namespace Glinter.Modules.Experiences.Domain.Entities;

public class ExperienceAvailability
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ExperienceId { get; set; }
    public Experience Experience { get; set; } = null!;
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int Capacity { get; set; }
    public decimal PricePerPerson { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public List<ExperienceBooking> Bookings { get; set; } = [];
}
