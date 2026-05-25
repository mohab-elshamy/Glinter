using Glinter.Modules.LocationCatalog.Application.Safety.Dtos;

namespace Glinter.Modules.LocationCatalog.Application.Abstractions;

public interface INewsProvider
{
    Task<List<NewsArticleDto>> SearchAsync(
        string query,
        int maxArticles,
        CancellationToken cancellationToken = default);
}