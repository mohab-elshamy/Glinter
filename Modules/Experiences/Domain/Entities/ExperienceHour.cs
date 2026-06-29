namespace Glinter.Modules.Experiences.Domain.Entities;

public class ExperienceHour
{
    public int Id { get; set; }
    public int ExperienceId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }

    public Experience Experience { get; set; } = null!;
}
