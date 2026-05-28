namespace Glinter.Modules.Experiences.Domain.Entities;

public class Vibe
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ExperienceVibe> ExperienceVibes { get; set; } = new List<ExperienceVibe>();
}