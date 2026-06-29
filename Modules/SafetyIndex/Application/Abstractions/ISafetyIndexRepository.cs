using Glinter.Modules.SafetyIndex.Domain.Entities;

namespace Glinter.Modules.SafetyIndex.Application.Abstractions;

public interface ISafetyIndexRepository
{
    Task<SafetyIndexResult?> GetByAdm2GidAsync(int adm2Gid, CancellationToken ct = default);

    Task<List<SafetyIndexResult>> GetByAdm2GidsAsync(IReadOnlyCollection<int> adm2Gids, CancellationToken ct = default);

    Task UpsertAsync(SafetyIndexResult result, CancellationToken ct = default);
}
