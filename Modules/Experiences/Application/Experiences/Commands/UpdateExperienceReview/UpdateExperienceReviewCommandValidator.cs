namespace Glinter.Modules.Experiences.Application.Experiences.Commands.UpdateExperienceReview;

public class UpdateExperienceReviewCommandValidator
{
    public void Validate(UpdateExperienceReviewCommand command)
    {
        if (command.ReviewId == Guid.Empty)
            throw new ValidationException("ReviewId is required.");

        if (command.Rating < 1 || command.Rating > 5)
            throw new ValidationException("Rating must be between 1 and 5.");

        if (!string.IsNullOrWhiteSpace(command.Comment) && command.Comment.Length > 2000)
            throw new ValidationException("Comment cannot exceed 2000 characters.");
    }
}