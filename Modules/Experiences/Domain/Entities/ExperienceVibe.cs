namespace Glinter.Modules.Experiences.Domain.Entities;

public class ExperienceVibe
{
    public Guid ExperienceId { get; set; }

    public Guid VibeId { get; set; }

    public Experience? Experience { get; set; }

    public Vibe? Vibe { get; set; }
}