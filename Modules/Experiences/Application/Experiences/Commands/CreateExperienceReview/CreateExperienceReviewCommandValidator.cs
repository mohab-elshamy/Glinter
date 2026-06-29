namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceReview;

public class CreateExperienceReviewCommandValidator
{
    public void Validate(CreateExperienceReviewCommand command)
    {
        if (command.ExperienceId == Guid.Empty)
            throw new ValidationException("ExperienceId is required.");

        if (command.Rating < 1 || command.Rating > 5)
            throw new ValidationException("Rating must be between 1 and 5.");

        if (!string.IsNullOrWhiteSpace(command.Comment) && command.Comment.Length > 2000)
            throw new ValidationException("Comment cannot exceed 2000 characters.");
    }
}