using Glinter.Modules.Experiences.Domain.Entities;

namespace Glinter.Modules.Experiences.Application.Abstractions;

public interface IExperienceReviewRepository
{
    Task AddAsync(ExperienceReview review, CancellationToken cancellationToken = default);

    Task<List<ExperienceReview>> GetByExperienceIdAsync(
        Guid experienceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ExperienceReview?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ExperienceReview?> GetForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid experienceId,
        Guid travelerProfileId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(ExperienceReview review, CancellationToken cancellationToken = default);

    Task DeleteAsync(ExperienceReview review, CancellationToken cancellationToken = default);
}
