using Glinter.Modules.Experiences.Domain.Entities;

namespace Glinter.Modules.Experiences.Application.Abstractions;

public interface IVibeRepository
{
    Task<List<Vibe>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<Vibe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAllAsync(IEnumerable<Guid> vibeIds, CancellationToken cancellationToken = default);
}