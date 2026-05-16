namespace Glinter.Modules.Experiences.Domain.Entities;

public class ExperienceReview
{
    public Guid Id { get; set; }

    public Guid ExperienceId { get; set; }

    public Guid TravelerProfileId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public Experience? Experience { get; set; }
}