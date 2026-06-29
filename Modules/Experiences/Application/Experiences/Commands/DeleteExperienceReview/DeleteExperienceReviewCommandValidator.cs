namespace Glinter.Modules.Experiences.Application.Experiences.Commands.DeleteExperienceReview;

public class DeleteExperienceReviewCommandValidator
{
    public void Validate(DeleteExperienceReviewCommand command)
    {
        if (command.ReviewId == Guid.Empty)
        {
            throw new ValidationException("ReviewId is required.");
        }
    }
}