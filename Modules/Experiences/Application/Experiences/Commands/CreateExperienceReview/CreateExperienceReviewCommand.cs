namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceReview;

public class CreateExperienceReviewCommand
{
    public Guid ExperienceId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }
}