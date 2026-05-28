using Glinter.Modules.Stays.Application.Abstractions;

namespace Glinter.Modules.Stays.Application.Reviews.Commands;

public class DeleteStayReviewHandler
{
    private readonly IStayReviewRepository _stayReviewRepository;

    public DeleteStayReviewHandler(IStayReviewRepository stayReviewRepository)
    {
        _stayReviewRepository = stayReviewRepository;
    }

    public async Task<bool> HandleAsync(
        DeleteStayReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        var review = await _stayReviewRepository.GetByIdAsync(command.ReviewId, cancellationToken);

        if (review is null)
            return false;

        if (review.TravelerProfileId != command.TravelerProfileId)
            throw new ArgumentException("Traveler is not allowed to delete this review.");

        await _stayReviewRepository.DeleteAsync(review, cancellationToken);
        return true;
    }
}