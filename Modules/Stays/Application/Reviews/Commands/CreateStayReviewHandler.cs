using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Reviews.Dtos;
using Glinter.Modules.Stays.Domain.Entities;

namespace Glinter.Modules.Stays.Application.Reviews.Commands;

public class CreateStayReviewHandler
{
    private readonly IStayRepository _stayRepository;
    private readonly IStayBookingRepository _stayBookingRepository;
    private readonly IStayReviewRepository _stayReviewRepository;

    public CreateStayReviewHandler(
        IStayRepository stayRepository,
        IStayBookingRepository stayBookingRepository,
        IStayReviewRepository stayReviewRepository)
    {
        _stayRepository = stayRepository;
        _stayBookingRepository = stayBookingRepository;
        _stayReviewRepository = stayReviewRepository;
    }

    public async Task<StayReviewResponseDto> HandleAsync(
        CreateStayReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Rating < 1 || command.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var stay = await _stayRepository.GetByIdAsync(command.StayId, cancellationToken);

        if (stay is null)
            throw new KeyNotFoundException("Stay not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var hasEligibleBooking = await _stayBookingRepository.HasEligibleReviewBookingAsync(
            command.StayId,
            command.TravelerProfileId,
            today,
            cancellationToken);

        if (!hasEligibleBooking)
            throw new ArgumentException("Traveler is not eligible to review this stay.");

        var alreadyReviewed = await _stayReviewRepository.ExistsAsync(
            command.StayId,
            command.TravelerProfileId,
            cancellationToken);

        if (alreadyReviewed)
            throw new ArgumentException("Traveler has already reviewed this stay.");

        var review = new StayReview
        {
            Id = Guid.NewGuid(),
            StayId = command.StayId,
            TravelerProfileId = command.TravelerProfileId,
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