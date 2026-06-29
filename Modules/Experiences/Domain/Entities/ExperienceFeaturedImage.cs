namespace Glinter.Modules.Experiences.Domain.Entities;

public class ExperienceFeaturedImage
{
    public int Id { get; set; }
    public int ExperienceId { get; set; }
    public string Link { get; set; } = string.Empty;

    public Experience Experience { get; set; } = null!;
}
