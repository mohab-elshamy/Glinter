using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.UpdateExperienceReview;

public class UpdateExperienceReviewCommandHandler
{
    private readonly IExperienceReviewRepository _reviewRepository;
    private readonly IExperienceProfileResolver _profileResolver;
    private readonly UpdateExperienceReviewCommandValidator _validator;

    public UpdateExperienceReviewCommandHandler(
        IExperienceReviewRepository reviewRepository,
        IExperienceProfileResolver profileResolver,
        UpdateExperienceReviewCommandValidator validator)
    {
        _reviewRepository = reviewRepository;
        _profileResolver = profileResolver;
        _validator = validator;
    }

    public async Task<ExperienceReviewResponseDto?> HandleAsync(
        UpdateExperienceReviewCommand command,
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
            return null;
        }

        if (review.TravelerProfileId != travelerProfileId)
        {
            throw new UnauthorizedAccessException("You can update only your own reviews.");
        }

        review.Rating = command.Rating;
        review.Comment = string.IsNullOrWhiteSpace(command.Comment)
            ? null
            : command.Comment.Trim();
        review.UpdatedAtUtc = DateTime.UtcNow;

        await _reviewRepository.UpdateAsync(review, cancellationToken);

        return ExperiencesMappings.ToReviewResponse(review);
    }
}