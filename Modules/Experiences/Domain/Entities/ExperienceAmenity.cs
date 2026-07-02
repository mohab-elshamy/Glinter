namespace Glinter.Modules.Experiences.Domain.Entities;

public class ExperienceAmenity
{
    public int Id { get; set; }
    public int ExperienceId { get; set; }
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }

    public Experience Experience { get; set; } = null!;
}
