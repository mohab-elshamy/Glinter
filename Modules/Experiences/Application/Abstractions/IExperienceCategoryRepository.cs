using Glinter.Modules.Experiences.Domain.Entities;

namespace Glinter.Modules.Experiences.Application.Abstractions;

public interface IExperienceCategoryRepository
{
    Task<List<ExperienceCategory>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<ExperienceCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}