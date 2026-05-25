namespace Glinter.Modules.LocationCatalog.Infrastructure.Options;

public sealed class NewsApiOptions
{
    public string BaseUrl { get; set; } = "https://newsapi.org/v2/";

    public string? ApiKey { get; set; }

    public string Language { get; set; } = "en";

    public string SortBy { get; set; } = "relevancy";
    
    public string SearchIn { get; set; } = "title,description";
}