using Glinter.Modules.Stays.Domain.Entities;

namespace Glinter.Modules.Stays.Application.Abstractions;

public interface IStayRepository
{
    Task<Stay> AddAsync(Stay stay, CancellationToken cancellationToken = default);
    Task<List<Stay>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Stay?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid ownerProfileId, string name, string address, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid ownerProfileId, string name, string address, Guid excludedStayId, CancellationToken cancellationToken = default);
    Task<Stay> UpdateAsync(Stay stay, CancellationToken cancellationToken = default);

    Task<Stay?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Stay>> GetByAdm3GidAsync(int adm3Gid, int page, int pageSize, CancellationToken cancellationToken = default);

    Task ReplaceTagsAsync(
        Guid stayId,
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default);

    Task<List<Stay>> GetFilteredAsync(
        int? adm3Gid,
        decimal? minPrice,
        decimal? maxPrice,
        int? guests,
        string? tag,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
