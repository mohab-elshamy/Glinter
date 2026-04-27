using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Reviews.Dtos;

namespace Glinter.Modules.Stays.Application.Reviews.Commands;

public class UpdateStayReviewHandler
{
    private readonly IStayReviewRepository _stayReviewRepository;

    public UpdateStayReviewHandler(IStayReviewRepository stayReviewRepository)
    {
        _stayReviewRepository = stayReviewRepository;
    }

    public async Task<StayReviewResponseDto?> HandleAsync(
        UpdateStayReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Rating < 1 || command.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var review = await _stayReviewRepository.GetByIdAsync(command.ReviewId, cancellationToken);

        if (review is null)
            return null;

        if (review.TravelerProfileId != command.TravelerProfileId)
            throw new ArgumentException("Traveler is not allowed to update this review.");

        review.Rating = command.Rating;
        review.Comment = command.Comment?.Trim() ?? string.Empty;

        var updatedReview = await _stayReviewRepository.UpdateAsync(review, cancellationToken);

        return new StayReviewResponseDto
        {
            Id = updatedReview.Id,
            StayId = updatedReview.StayId,
            TravelerProfileId = updatedReview.TravelerProfileId,
            Rating = updatedReview.Rating,
            Comment = updatedReview.Comment,
            CreatedAtUtc = updatedReview.CreatedAtUtc
        };
    }
}