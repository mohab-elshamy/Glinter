using System.Text.Json;
using Glinter.Modules.LocationCatalog.Application.Abstractions;
using Glinter.Modules.LocationCatalog.Application.Safety.Dtos;
using Glinter.Modules.LocationCatalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.LocationCatalog.Application.Safety.Services;

public sealed class DistrictSafetySignalService
{
    private readonly ILocationCatalogDbContext _dbContext;
    private readonly ISafetyAiAnalyzer _safetyAiAnalyzer;

    public DistrictSafetySignalService(
        ILocationCatalogDbContext dbContext,
        ISafetyAiAnalyzer safetyAiAnalyzer)
    {
        _dbContext = dbContext;
        _safetyAiAnalyzer = safetyAiAnalyzer;
    }

    public async Task<DistrictSafetySignalResponse> AnalyzeAndSaveAsync(
        Guid districtId,
        AnalyzeDistrictSafetySignalRequest request,
        CancellationToken cancellationToken = default)
    {
        var district = await _dbContext.Districts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == districtId, cancellationToken);

        if (district is null)
        {
            throw new InvalidOperationException("District was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Content is required.");
        }

        var textToAnalyze = $"{request.Title}\n\n{request.Content}";

        var aiResult = await _safetyAiAnalyzer.AnalyzeAsync(
            district.NameEn,
            textToAnalyze,
            cancellationToken);

        var signal = new DistrictSafetySignal
        {
            Id = Guid.NewGuid(),
            DistrictId = districtId,

            SourceType = Truncate(
                string.IsNullOrWhiteSpace(request.SourceType)
                    ? "Manual"
                    : request.SourceType.Trim(),
                100),

            Title = Truncate(request.Title.Trim(), 500),

            Content = Truncate(request.Content.Trim(), 4000),

            SourceUrl = TruncateNullable(request.SourceUrl, 1000),

            RiskCategory = Truncate(aiResult.RiskCategory, 100),

            Severity = Truncate(aiResult.Severity, 50),

            Confidence = aiResult.Confidence,
            SentimentScore = aiResult.SentimentScore,
            IsSafetyRelevant = aiResult.IsSafetyRelevant,

            AiSummary = Truncate(aiResult.Summary, 1000),

            RawAiJson = Truncate(JsonSerializer.Serialize(aiResult), 4000),

            PublishedAtUtc = request.PublishedAtUtc ?? DateTime.UtcNow,
            AnalyzedAtUtc = DateTime.UtcNow
        };

        _dbContext.DistrictSafetySignals.Add(signal);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DistrictSafetySignalResponse(
            signal.Id,
            signal.DistrictId,
            signal.SourceType,
            signal.Title,
            signal.RiskCategory,
            signal.Severity,
            signal.Confidence,
            signal.SentimentScore,
            signal.IsSafetyRelevant,
            signal.AiSummary,
            signal.PublishedAtUtc,
            signal.AnalyzedAtUtc
        );
    }
    
    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();

        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength];
    }

    private static string? TruncateNullable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength];
    }
}