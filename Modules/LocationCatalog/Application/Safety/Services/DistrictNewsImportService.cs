using Glinter.Modules.LocationCatalog.Application.Abstractions;
using Glinter.Modules.LocationCatalog.Application.Safety.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.LocationCatalog.Application.Safety.Services;

public sealed class DistrictNewsImportService
{
    private readonly ILocationCatalogDbContext _dbContext;
    private readonly INewsProvider _newsProvider;
    private readonly DistrictSafetySignalService _districtSafetySignalService;
    private readonly DistrictSafetyScoreService _districtSafetyScoreService;

    public DistrictNewsImportService(
        ILocationCatalogDbContext dbContext,
        INewsProvider newsProvider,
        DistrictSafetySignalService districtSafetySignalService,
        DistrictSafetyScoreService districtSafetyScoreService)
    {
        _dbContext = dbContext;
        _newsProvider = newsProvider;
        _districtSafetySignalService = districtSafetySignalService;
        _districtSafetyScoreService = districtSafetyScoreService;
    }

    public async Task<ImportDistrictNewsResponse> ImportAsync(
        Guid districtId,
        ImportDistrictNewsRequest request,
        CancellationToken cancellationToken = default)
    {
        var district = await _dbContext.Districts
            .AsNoTracking()
            .Include(x => x.Governorate)
            .ThenInclude(x => x.Country)
            .FirstOrDefaultAsync(x => x.Id == districtId, cancellationToken);

        if (district is null)
        {
            throw new InvalidOperationException("District was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ArgumentException("News query is required.");
        }

        var maxArticles = Math.Clamp(request.MaxArticles, 1, 10);

        var articles = await _newsProvider.SearchAsync(
            request.Query,
            maxArticles,
            cancellationToken);

        var fetchedTitles = articles
            .Select(x => x.Title)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var analyzedTitles = new List<string>();

        var analyzedCount = 0;
        var skippedCount = 0;
        var relevantCount = 0;

        foreach (var article in articles)
        {
            var content = BuildArticleContent(article);

            if (string.IsNullOrWhiteSpace(content))
            {
                skippedCount++;
                continue;
            }

            var relevanceText = $"{article.Title}\n{article.Description}";

            if (!IsArticleRelatedToDistrict(
                    relevanceText,
                    district.NameEn,
                    district.NameAr,
                    district.Governorate.NameEn,
                    district.Governorate.NameAr,
                    district.Governorate.Country.NameEn,
                    district.Governorate.Country.NameAr))
            {
                skippedCount++;
                continue;
            }

            var signal = await _districtSafetySignalService.AnalyzeAndSaveAsync(
                districtId,
                new AnalyzeDistrictSafetySignalRequest(
                    SourceType: "NewsAPI",
                    Title: article.Title,
                    Content: content,
                    SourceUrl: article.Url,
                    PublishedAtUtc: article.PublishedAtUtc
                ),
                cancellationToken);

            analyzedCount++;
            analyzedTitles.Add(article.Title);

            if (signal.IsSafetyRelevant)
            {
                relevantCount++;
            }
        }

        var score = await _districtSafetyScoreService.CalculateAndSaveAsync(
            districtId,
            cancellationToken);

        return new ImportDistrictNewsResponse(
            DistrictId: district.Id,
            DistrictNameEn: district.NameEn,
            Query: request.Query,
            FetchedArticles: articles.Count,
            AnalyzedArticles: analyzedCount,
            SkippedArticles: skippedCount,
            SafetyRelevantArticles: relevantCount,
            SafetyScore: score.SafetyScore,
            SafetyLevel: score.SafetyLevel,
            Explanation: score.Explanation,
            FetchedArticleTitles: fetchedTitles,
            AnalyzedArticleTitles: analyzedTitles
        );
    }

    private static string BuildArticleContent(NewsArticleDto article)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(article.Description))
        {
            parts.Add(article.Description.Trim());
        }

        if (!string.IsNullOrWhiteSpace(article.Content))
        {
            parts.Add(article.Content.Trim());
        }

        if (!string.IsNullOrWhiteSpace(article.SourceName))
        {
            parts.Add($"Source: {article.SourceName}");
        }

        if (!string.IsNullOrWhiteSpace(article.Url))
        {
            parts.Add($"Url: {article.Url}");
        }

        return string.Join("\n", parts);
    }

    private static bool IsArticleRelatedToDistrict(
        string text,
        string districtNameEn,
        string? districtNameAr,
        string governorateNameEn,
        string? governorateNameAr,
        string countryNameEn,
        string? countryNameAr)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalizedText = text.ToLower();

        var districtKeywords = new List<string>
        {
            districtNameEn.ToLower()
        };

        if (!string.IsNullOrWhiteSpace(districtNameAr))
        {
            districtKeywords.Add(districtNameAr);
        }

        districtKeywords.AddRange(GetDistrictAliases(districtNameEn));

        var locationContextKeywords = new List<string>
        {
            governorateNameEn.ToLower(),
            countryNameEn.ToLower()
        };

        if (!string.IsNullOrWhiteSpace(governorateNameAr))
        {
            locationContextKeywords.Add(governorateNameAr);
        }

        if (!string.IsNullOrWhiteSpace(countryNameAr))
        {
            locationContextKeywords.Add(countryNameAr);
        }

        locationContextKeywords.AddRange([
            "cairo",
            "egypt",
            "القاهرة",
            "مصر"
        ]);

        var hasDistrictKeyword = districtKeywords.Any(keyword =>
            !string.IsNullOrWhiteSpace(keyword) &&
            normalizedText.Contains(keyword.ToLower()));

        var hasLocationContext = locationContextKeywords.Any(keyword =>
            !string.IsNullOrWhiteSpace(keyword) &&
            normalizedText.Contains(keyword.ToLower()));

        return hasDistrictKeyword && hasLocationContext;
    }

    private static List<string> GetDistrictAliases(string districtNameEn)
    {
        return districtNameEn switch
        {
            "Maadi" =>
            [
                "maadi",
                "el maadi",
                "al maadi",
                "المعادي",
                "معادي"
            ],

            "Zamalik" =>
            [
                "zamalek",
                "zamalik",
                "الزمالك"
            ],

            "Nasr City" =>
            [
                "nasr city",
                "madinat nasr",
                "مدينة نصر"
            ],

            "Misr al-Gadida" =>
            [
                "heliopolis",
                "misr el gedida",
                "مصر الجديدة"
            ],

            _ => []
        };
    }
}