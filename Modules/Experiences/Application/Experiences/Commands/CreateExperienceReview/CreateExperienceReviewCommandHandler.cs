using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Domain.Entities;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceReview;

public class CreateExperienceReviewCommandHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceBookingRepository _bookingRepository;
    private readonly IExperienceReviewRepository _reviewRepository;
    private readonly IExperienceProfileResolver _profileResolver;
    private readonly CreateExperienceReviewCommandValidator _validator;

    public CreateExperienceReviewCommandHandler(
        IExperienceRepository experienceRepository,
        IExperienceBookingRepository bookingRepository,
        IExperienceReviewRepository reviewRepository,
        IExperienceProfileResolver profileResolver,
        CreateExperienceReviewCommandValidator validator)
    {
        _experienceRepository = experienceRepository;
        _bookingRepository = bookingRepository;
        _reviewRepository = reviewRepository;
        _profileResolver = profileResolver;
        _validator = validator;
    }

    public async Task<ExperienceReviewResponseDto?> HandleAsync(
        CreateExperienceReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        _validator.Validate(command);

        var travelerProfileId = await _profileResolver
            .GetCurrentTravelerProfileIdAsync(cancellationToken);

        var experience = await _experienceRepository.GetByIdAsync(
            command.ExperienceId,
            cancellationToken);

        if (experience == null)
        {
            return null;
        }

        var hasCompletedBooking = await _bookingRepository.HasCompletedBookingAsync(
            command.ExperienceId,
            travelerProfileId,
            cancellationToken);

        if (!hasCompletedBooking)
        {
            throw new InvalidOperationException("You can review this experience only after completing a booking.");
        }

        var alreadyReviewed = await _reviewRepository.ExistsAsync(
            command.ExperienceId,
            travelerProfileId,
            cancellationToken);

        if (alreadyReviewed)
        {
            throw new InvalidOperationException("You have already reviewed this experience.");
        }

        var review = new ExperienceReview
        {
            Id = Guid.NewGuid(),
            ExperienceId = command.ExperienceId,
            TravelerProfileId = travelerProfileId,
            Rating = command.Rating,
            Comment = string.IsNullOrWhiteSpace(command.Comment)
                ? null
                : command.Comment.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _reviewRepository.AddAsync(review, cancellationToken);

        return ExperiencesMappings.ToReviewResponse(review);
    }
}