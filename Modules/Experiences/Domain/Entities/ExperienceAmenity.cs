namespace Glinter.Modules.Experiences.Domain.Entities;

public class ExperienceAmenity
{
    public int Id { get; set; }
    public int ExperienceId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Experience Experience { get; set; } = null!;
}
