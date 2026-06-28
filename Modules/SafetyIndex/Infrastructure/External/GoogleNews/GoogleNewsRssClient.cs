using System.Globalization;
using System.Net;
using System.Xml.Linq;
using Glinter.Modules.SafetyIndex.Application.Abstractions;
using Glinter.Modules.SafetyIndex.Application.Dtos;
using Glinter.Modules.SafetyIndex.Application.Options;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.SafetyIndex.Infrastructure.External.GoogleNews;

public class GoogleNewsRssClient(
    HttpClient httpClient,
    IOptions<SafetyIndexOptions> options,
    ILogger<GoogleNewsRssClient> logger) : IGoogleNewsRssClient
{
    private readonly SafetyIndexOptions options = options.Value;

    public async Task<List<NewsItemDto>> SearchAsync(
        string arabicAreaName,
        int? lookbackDays,
        CancellationToken ct = default)
    {
        var queryParts = new List<string> { $"\"{arabicAreaName}\"" };
        if (!string.IsNullOrWhiteSpace(options.QueryCountryHint))
        {
            queryParts.Add(options.QueryCountryHint);
        }

        if (lookbackDays is > 0)
        {
            queryParts.Add($"after:{DateTime.UtcNow.AddDays(-lookbackDays.Value):yyyy-MM-dd}");
        }

        var query = string.Join(' ', queryParts);
        var uriBuilder = new UriBuilder(options.GoogleNewsRssBaseUrl);
        uriBuilder.Query = string.Join('&',
        [
            $"q={Uri.EscapeDataString(query)}",
            $"hl={Uri.EscapeDataString(options.GoogleNewsHl)}",
            $"gl={Uri.EscapeDataString(options.GoogleNewsGl)}",
            $"ceid={Uri.EscapeDataString(options.GoogleNewsCeid)}",
        ]);

        logger.LogInformation(
            "Fetching Google News RSS for area {AreaNameAr} with lookback {LookbackDays}",
            arabicAreaName,
            lookbackDays);

        using var response = await httpClient.GetAsync(uriBuilder.Uri, ct);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync(ct);
        return ParseRss(xml);
    }

    private static List<NewsItemDto> ParseRss(string xml)
    {
        var document = XDocument.Parse(xml);

        return document.Descendants("item")
            .Select(ParseItem)
            .Where(x => !string.IsNullOrWhiteSpace(x.Title))
            .ToList();
    }

    private static NewsItemDto ParseItem(XElement item)
    {
        var source = item.Element("source");
        var pubDate = WebUtility.HtmlDecode(item.Element("pubDate")?.Value ?? string.Empty);

        return new NewsItemDto
        {
            Title = WebUtility.HtmlDecode(item.Element("title")?.Value ?? string.Empty).Trim(),
            Link = WebUtility.HtmlDecode(item.Element("link")?.Value ?? string.Empty).Trim(),
            Guid = WebUtility.HtmlDecode(item.Element("guid")?.Value ?? string.Empty).Trim(),
            Description = WebUtility.HtmlDecode(item.Element("description")?.Value ?? string.Empty).Trim(),
            SourceName = WebUtility.HtmlDecode(source?.Value ?? string.Empty).Trim(),
            SourceUrl = WebUtility.HtmlDecode(source?.Attribute("url")?.Value ?? string.Empty).Trim(),
            PublishedAtUtc = DateTimeOffset.TryParse(
                pubDate,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var parsed)
                ? parsed.ToUniversalTime()
                : null,
            FetchedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}
