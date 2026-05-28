namespace Glinter.Modules.Experiences.Domain.Entities;

public class ExperienceCategory
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Experience> Experiences { get; set; } = new List<Experience>();
}