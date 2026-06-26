using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Reviews.Dtos;
using Glinter.Modules.Stays.Domain.Entities;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;

namespace Glinter.Modules.Stays.Application.Reviews.Commands;

public class CreateStayReviewHandler
{
    private readonly IStayRepository _stayRepository;
    private readonly IStayBookingRepository _stayBookingRepository;
    private readonly IStayReviewRepository _stayReviewRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProfilesReadService _profilesReadService;

    public CreateStayReviewHandler(
        IStayRepository stayRepository,
        IStayBookingRepository stayBookingRepository,
        IStayReviewRepository stayReviewRepository,
        ICurrentUserService currentUserService,
        IProfilesReadService profilesReadService)
    {
        _stayRepository = stayRepository;
        _stayBookingRepository = stayBookingRepository;
        _stayReviewRepository = stayReviewRepository;
        _currentUserService = currentUserService;
        _profilesReadService = profilesReadService;
    }

    public async Task<StayReviewResponseDto> HandleAsync(
        CreateStayReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Rating < 1 || command.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        if ((command.Comment?.Trim().Length ?? 0) > 2000)
            throw new ArgumentException("Comment cannot exceed 2000 characters.");

        var stay = await _stayRepository.GetByIdAsync(command.StayId, cancellationToken);

        if (stay is null)
            throw new KeyNotFoundException("Stay not found.");

        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var currentUserId = _currentUserService.UserId.Value;

        var travelerProfileId = await _profilesReadService
            .GetTravelerProfileIdByUserIdAsync(currentUserId, cancellationToken);

        if (travelerProfileId is null)
        {
            throw new UnauthorizedAccessException("Only travelers can review stays.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var hasEligibleBooking = await _stayBookingRepository.HasEligibleReviewBookingAsync(
            command.StayId,
            travelerProfileId.Value,
            today,
            cancellationToken);

        if (!hasEligibleBooking)
            throw new ArgumentException("Traveler is not eligible to review this stay.");

        var alreadyReviewed = await _stayReviewRepository.ExistsAsync(
            command.StayId,
            travelerProfileId.Value,
            cancellationToken);

        if (alreadyReviewed)
            throw new ArgumentException("Traveler has already reviewed this stay.");

        var review = new StayReview
        {
            Id = Guid.NewGuid(),
            StayId = command.StayId,
            TravelerProfileId = travelerProfileId.Value,
            Rating = command.Rating,
            Comment = command.Comment?.Trim() ?? string.Empty,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createdReview = await _stayReviewRepository.AddAsync(review, cancellationToken);

        return new StayReviewResponseDto
        {
            Id = createdReview.Id,
            StayId = createdReview.StayId,
            TravelerProfileId = createdReview.TravelerProfileId,
            Rating = createdReview.Rating,
            Comment = createdReview.Comment,
            CreatedAtUtc = createdReview.CreatedAtUtc
        };
    }
}
