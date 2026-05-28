namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceReview;

public class CreateExperienceReviewCommandValidator
{
    public void Validate(CreateExperienceReviewCommand command)
    {
        if (command.ExperienceId == Guid.Empty)
            throw new ArgumentException("ExperienceId is required.");

        if (command.Rating < 1 || command.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        if (!string.IsNullOrWhiteSpace(command.Comment) && command.Comment.Length > 2000)
            throw new ArgumentException("Comment cannot exceed 2000 characters.");
    }
}