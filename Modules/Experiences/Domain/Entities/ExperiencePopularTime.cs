namespace Glinter.Modules.Experiences.Domain.Entities;

public class ExperiencePopularTime
{
    public int Id { get; set; }
    public int ExperienceId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public int HourOfDay { get; set; }
    public int PopularityPercentage { get; set; }

    public Experience Experience { get; set; } = null!;
}
