using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Glinter.Modules.LocationCatalog.Application.Abstractions;
using Glinter.Modules.LocationCatalog.Application.Safety.Dtos;
using Glinter.Modules.LocationCatalog.Infrastructure.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.LocationCatalog.Infrastructure.Services;

public sealed class NewsApiNewsProvider : INewsProvider
{
    private readonly HttpClient _httpClient;
    private readonly NewsApiOptions _options;

    public NewsApiNewsProvider(
        HttpClient httpClient,
        IOptions<NewsApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<List<NewsArticleDto>> SearchAsync(
        string query,
        int maxArticles,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("News query is required.");
        }

        var apiKey = ResolveApiKey();

        var safeMaxArticles = Math.Clamp(maxArticles, 1, 10);

        var url = QueryHelpers.AddQueryString("everything", new Dictionary<string, string?>
        {
            ["q"] = query.Trim(),
            ["searchIn"] = string.IsNullOrWhiteSpace(_options.SearchIn)
                ? "title,description"
                : _options.SearchIn,
            ["language"] = string.IsNullOrWhiteSpace(_options.Language)
                ? "en"
                : _options.Language,
            ["sortBy"] = string.IsNullOrWhiteSpace(_options.SortBy)
                ? "relevancy"
                : _options.SortBy,
            ["pageSize"] = safeMaxArticles.ToString()
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.Add("X-Api-Key", apiKey);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Glinter", "1.0"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        var result = await response.Content
            .ReadFromJsonAsync<NewsApiResponse>(cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = result?.Message ?? $"NewsAPI request failed with status {(int)response.StatusCode}.";
            throw new InvalidOperationException(message);
        }

        if (result is null || !string.Equals(result.Status, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(result?.Message ?? "NewsAPI returned an invalid response.");
        }

        return result.Articles
            .Where(x => !string.IsNullOrWhiteSpace(x.Title))
            .Select(x => new NewsArticleDto(
                Title: x.Title!.Trim(),
                Description: x.Description,
                Content: x.Content,
                Url: x.Url,
                PublishedAtUtc: x.PublishedAt,
                SourceName: x.Source?.Name ?? "NewsAPI"
            ))
            .ToList();
    }

    private string ResolveApiKey()
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return _options.ApiKey;
        }

        var apiKey = Environment.GetEnvironmentVariable("NEWS_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "NEWS_API_KEY was not found. Add it as an environment variable before running the app.");
        }

        return apiKey;
    }

    private sealed class NewsApiResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("totalResults")]
        public int TotalResults { get; set; }

        [JsonPropertyName("articles")]
        public List<NewsApiArticle> Articles { get; set; } = [];
    }

    private sealed class NewsApiArticle
    {
        [JsonPropertyName("source")]
        public NewsApiSource? Source { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }
    }

    private sealed class NewsApiSource
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}