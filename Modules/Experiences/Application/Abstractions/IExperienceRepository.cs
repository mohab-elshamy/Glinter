using Glinter.Modules.Experiences.Domain.Entities;
using Glinter.Modules.Experiences.Domain.Enums;

namespace Glinter.Modules.Experiences.Application.Abstractions;

public interface IExperienceRepository
{
    Task AddAsync(Experience experience, CancellationToken cancellationToken = default);

    Task<Experience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Experience?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<Experience>> GetFilteredAsync(
        Guid? areaId,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        int? guests,
        Guid? vibeId,
        string? tag,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid providerProfileId,
        string title,
        Guid areaId,
        CancellationToken cancellationToken = default);

    Task ReplaceTagsAsync(
        Guid experienceId,
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default);

    Task ReplaceVibesAsync(
        Guid experienceId,
        IEnumerable<Guid> vibeIds,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Experience experience, CancellationToken cancellationToken = default);
    
    Task<List<Experience>> GetByProviderProfileIdAsync(
        Guid providerProfileId,
        CancellationToken cancellationToken = default);
    
    Task<Experience?> GetPublishedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<Experience>> GetForAdminAsync(
        ExperienceApprovalStatus? approvalStatus,
        bool? isActive,
        CancellationToken cancellationToken = default);
}