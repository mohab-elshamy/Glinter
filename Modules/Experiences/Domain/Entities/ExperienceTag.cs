namespace Glinter.Modules.Experiences.Domain.Entities;

public class ExperienceTag
{
    public Guid Id { get; set; }

    public Guid ExperienceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Experience? Experience { get; set; }
}