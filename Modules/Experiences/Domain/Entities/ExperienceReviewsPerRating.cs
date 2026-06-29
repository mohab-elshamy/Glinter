namespace Glinter.Modules.Experiences.Domain.Entities;

public class ExperienceReviewsPerRating
{
    public int Id { get; set; }
    public int ExperienceId { get; set; }
    public int Rating { get; set; }
    public int ReviewsCount { get; set; }

    public Experience Experience { get; set; } = null!;
}
