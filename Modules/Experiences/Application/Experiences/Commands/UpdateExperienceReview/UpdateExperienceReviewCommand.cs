namespace Glinter.Modules.Experiences.Application.Experiences.Commands.UpdateExperienceReview;

public class UpdateExperienceReviewCommand
{
    public Guid ReviewId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }
}