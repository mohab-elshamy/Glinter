using Glinter.Modules.Experiences.Domain.Entities;

namespace Glinter.Modules.Experiences.Application.Abstractions;

public interface IExperienceAvailabilityRepository
{
    Task AddAsync(ExperienceAvailability availability, CancellationToken cancellationToken = default);

    Task<List<ExperienceAvailability>> GetByExperienceIdAsync(
        Guid experienceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ExperienceAvailability?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ExperienceAvailability?> GetForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlapAsync(
        Guid experienceId,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ExperienceAvailability availability,
        CancellationToken cancellationToken = default);
}
