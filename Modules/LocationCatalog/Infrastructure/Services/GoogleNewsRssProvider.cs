using System.Globalization;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using Glinter.Modules.LocationCatalog.Application.Abstractions;
using Glinter.Modules.LocationCatalog.Application.Safety.Dtos;
using Glinter.Modules.LocationCatalog.Infrastructure.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.LocationCatalog.Infrastructure.Services;

public sealed class GoogleNewsRssProvider : INewsProvider
{
    private readonly HttpClient _httpClient;
    private readonly GoogleNewsRssOptions _options;

    public GoogleNewsRssProvider(
        HttpClient httpClient,
        IOptions<GoogleNewsRssOptions> options)
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

        var safeMaxArticles = Math.Clamp(maxArticles, 1, 20);

        var isArabicQuery = ContainsArabic(query);

        var url = QueryHelpers.AddQueryString(_options.SearchUrl, new Dictionary<string, string?>
        {
            ["q"] = query.Trim(),
            ["hl"] = isArabicQuery ? _options.ArabicHl : _options.EnglishHl,
            ["gl"] = isArabicQuery ? _options.ArabicGl : _options.EnglishGl,
            ["ceid"] = isArabicQuery ? _options.ArabicCeid : _options.EnglishCeid
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("Glinter/1.0");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        var rssXml = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Google News RSS request failed with status {(int)response.StatusCode}.");
        }

        if (string.IsNullOrWhiteSpace(rssXml))
        {
            return [];
        }

        using var stringReader = new StringReader(rssXml);
        using var xmlReader = XmlReader.Create(stringReader);

        var feed = SyndicationFeed.Load(xmlReader);

        if (feed is null)
        {
            return [];
        }

        return feed.Items
            .Take(safeMaxArticles)
            .Where(x => !string.IsNullOrWhiteSpace(x.Title?.Text))
            .Select(x =>
            {
                var title = CleanText(x.Title.Text);
                var description = CleanText(x.Summary?.Text);
                var url = x.Links.FirstOrDefault()?.Uri?.ToString();
                var publishedAt = x.PublishDate == DateTimeOffset.MinValue
                    ? (DateTime?)null
                    : x.PublishDate.UtcDateTime;

                return new NewsArticleDto(
                    Title: title,
                    Description: description,
                    Content: null,
                    Url: url,
                    PublishedAtUtc: publishedAt,
                    SourceName: "Google News RSS"
                );
            })
            .ToList();
    }

    private static bool ContainsArabic(string value)
    {
        return value.Any(c => c >= '\u0600' && c <= '\u06FF');
    }

    private static string? CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var decoded = WebUtility.HtmlDecode(value);

        var withoutHtml = Regex.Replace(decoded, "<.*?>", string.Empty);

        return withoutHtml.Trim();
    }
}