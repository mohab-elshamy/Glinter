using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Domain.Enums;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceReviews;

public class GetExperienceReviewsQueryHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceReviewRepository _reviewRepository;

    public GetExperienceReviewsQueryHandler(
        IExperienceRepository experienceRepository,
        IExperienceReviewRepository reviewRepository)
    {
        _experienceRepository = experienceRepository;
        _reviewRepository = reviewRepository;
    }

    public async Task<List<ExperienceReviewResponseDto>?> HandleAsync(
        GetExperienceReviewsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.ExperienceId == Guid.Empty)
        {
            throw new ArgumentException("ExperienceId is required.");
        }

        var experience = await _experienceRepository.GetByIdAsync(
            query.ExperienceId,
            cancellationToken);

        if (experience == null)
        {
            return null;
        }

        if (!experience.IsActive || experience.ApprovalStatus != ExperienceApprovalStatus.Approved)
        {
            return null;
        }

        var reviews = await _reviewRepository.GetByExperienceIdAsync(
            query.ExperienceId,
            cancellationToken);

        return reviews
            .Select(ExperiencesMappings.ToReviewResponse)
            .ToList();
    }
}