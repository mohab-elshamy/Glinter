using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Reviews.Dtos;

namespace Glinter.Modules.Stays.Application.Reviews.Commands;

public class UpdateStayReviewHandler
{
    private readonly IStayReviewRepository _stayReviewRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProfilesReadService _profilesReadService;

    public UpdateStayReviewHandler(
        IStayReviewRepository stayReviewRepository,
        ICurrentUserService currentUserService,
        IProfilesReadService profilesReadService)
    {
        _stayReviewRepository = stayReviewRepository;
        _currentUserService = currentUserService;
        _profilesReadService = profilesReadService;
    }

    public async Task<StayReviewResponseDto?> HandleAsync(
        UpdateStayReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ReviewId == Guid.Empty)
            throw new ArgumentException("Review id is required.");

        if (command.Rating < 1 || command.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        if ((command.Comment?.Trim().Length ?? 0) > 2000)
            throw new ArgumentException("Comment cannot exceed 2000 characters.");

        var travelerProfileId = await GetCurrentTravelerProfileIdAsync(cancellationToken);

        var review = await _stayReviewRepository.GetByIdAsync(command.ReviewId, cancellationToken);

        if (review is null)
            return null;

        if (review.TravelerProfileId != travelerProfileId)
            throw new UnauthorizedAccessException("You can update only your own reviews.");

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

    private async Task<Guid> GetCurrentTravelerProfileIdAsync(CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var travelerProfileId = await _profilesReadService.GetTravelerProfileIdByUserIdAsync(
            _currentUserService.UserId.Value,
            cancellationToken);

        return travelerProfileId
               ?? throw new UnauthorizedAccessException("Only travelers can update stay reviews.");
    }
}
