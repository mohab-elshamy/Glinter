using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Stays.Application.Abstractions;

namespace Glinter.Modules.Stays.Application.Reviews.Commands;

public class DeleteStayReviewHandler
{
    private readonly IStayReviewRepository _stayReviewRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProfilesReadService _profilesReadService;

    public DeleteStayReviewHandler(
        IStayReviewRepository stayReviewRepository,
        ICurrentUserService currentUserService,
        IProfilesReadService profilesReadService)
    {
        _stayReviewRepository = stayReviewRepository;
        _currentUserService = currentUserService;
        _profilesReadService = profilesReadService;
    }

    public async Task<bool> HandleAsync(
        DeleteStayReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ReviewId == Guid.Empty)
            throw new ValidationException("Review id is required.");

        var travelerProfileId = await GetCurrentTravelerProfileIdAsync(cancellationToken);

        var review = await _stayReviewRepository.GetByIdAsync(command.ReviewId, cancellationToken);

        if (review is null)
            return false;

        if (review.TravelerProfileId != travelerProfileId)
            throw new ForbiddenException("You can delete only your own reviews.");

        await _stayReviewRepository.DeleteAsync(review, cancellationToken);
        return true;
    }

    private async Task<Guid> GetCurrentTravelerProfileIdAsync(CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");

        var travelerProfileId = await _profilesReadService.GetTravelerProfileIdByUserIdAsync(
            _currentUserService.UserId.Value,
            cancellationToken);

        return travelerProfileId
               ?? throw new ForbiddenException("Only travelers can delete stay reviews.");
    }
}
