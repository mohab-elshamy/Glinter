using Glinter.Modules.SafetyIndex.Application.Dtos;

namespace Glinter.Modules.SafetyIndex.Application.Abstractions;

public interface ISafetyIndexService
{
    Task<Adm2SafetyIndexResponseDto?> GetByAdm2Async(int adm2Gid, CancellationToken ct = default);

    Task<List<Adm2SafetyIndexResponseDto>> GetByAdm1Async(int adm1Gid, CancellationToken ct = default);

    Task<List<Adm2SafetyIndexResponseDto>> GetByAdm0Async(int adm0Gid, CancellationToken ct = default);

    Task RefreshWeeklyAsync(CancellationToken ct = default);

    Task RunInitialHistoricalCollectionAsync(CancellationToken ct = default);
}
