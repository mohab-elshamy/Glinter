using Glinter.Modules.Experiences.Application.Abstractions;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.DeleteExperienceReview;

public class DeleteExperienceReviewCommandHandler
{
    private readonly IExperienceReviewRepository _reviewRepository;
    private readonly IExperienceProfileResolver _profileResolver;
    private readonly DeleteExperienceReviewCommandValidator _validator;

    public DeleteExperienceReviewCommandHandler(
        IExperienceReviewRepository reviewRepository,
        IExperienceProfileResolver profileResolver,
        DeleteExperienceReviewCommandValidator validator)
    {
        _reviewRepository = reviewRepository;
        _profileResolver = profileResolver;
        _validator = validator;
    }

    public async Task<bool> HandleAsync(
        DeleteExperienceReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        _validator.Validate(command);

        var travelerProfileId = await _profileResolver
            .GetCurrentTravelerProfileIdAsync(cancellationToken);

        var review = await _reviewRepository.GetForUpdateAsync(
            command.ReviewId,
            cancellationToken);

        if (review == null)
        {
            return false;
        }

        if (review.TravelerProfileId != travelerProfileId)
        {
            throw new ForbiddenException("You can delete only your own reviews.");
        }

        await _reviewRepository.DeleteAsync(review, cancellationToken);

        return true;
    }
}