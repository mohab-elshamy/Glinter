using Glinter.Modules.SafetyIndex.Application.Dtos;

namespace Glinter.Modules.SafetyIndex.Application.Abstractions;

public interface INewsHistoryStore
{
    Task<List<NewsItemDto>> MergeAsync(int adm2Gid, IEnumerable<NewsItemDto> newsItems, CancellationToken ct = default);

    Task<List<NewsItemDto>> ReadAsync(int adm2Gid, CancellationToken ct = default);
}
