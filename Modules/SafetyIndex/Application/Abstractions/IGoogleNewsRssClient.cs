using Glinter.Modules.SafetyIndex.Application.Dtos;

namespace Glinter.Modules.SafetyIndex.Application.Abstractions;

public interface IGoogleNewsRssClient
{
    Task<List<NewsItemDto>> SearchAsync(
        string arabicAreaName,
        int? lookbackDays,
        CancellationToken ct = default);
}
