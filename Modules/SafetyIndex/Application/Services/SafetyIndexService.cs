using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using Glinter.Modules.SafetyIndex.Application.Abstractions;
using Glinter.Modules.SafetyIndex.Application.Dtos;
using Glinter.Modules.SafetyIndex.Application.Options;
using Glinter.Modules.SafetyIndex.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.SafetyIndex.Application.Services;

public class SafetyIndexService(
    IAdm1Repository adm1Repository,
    IAdm2Repository adm2Repository,
    ISafetyIndexRepository safetyIndexRepository,
    IGoogleNewsRssClient googleNewsRssClient,
    INewsHistoryStore newsHistoryStore,
    ISafetyScoringClient safetyScoringClient,
    IOptions<SafetyIndexOptions> options,
    ILogger<SafetyIndexService> logger) : ISafetyIndexService
{
    private readonly SafetyIndexOptions options = options.Value;

    public async Task<Adm2SafetyIndexResponseDto?> GetByAdm2Async(int adm2Gid, CancellationToken ct = default)
    {
        var area = await adm2Repository.GetByIdAsync(adm2Gid, false, ct);
        if (area is null)
        {
            return null;
        }

        var result = await safetyIndexRepository.GetByAdm2GidAsync(adm2Gid, ct);
        return Map(area, result);
    }

    public async Task<List<Adm2SafetyIndexResponseDto>> GetByAdm1Async(int adm1Gid, CancellationToken ct = default)
    {
        var areas = await GetAllAdm2ByAdm1Async(adm1Gid, ct);
        return await MapManyAsync(areas, ct);
    }

    public async Task<List<Adm2SafetyIndexResponseDto>> GetByAdm0Async(int adm0Gid, CancellationToken ct = default)
    {
        var governorates = await GetAllAdm1ByAdm0Async(adm0Gid, ct);
        var areas = new List<Adm2>();

        foreach (var governorate in governorates)
        {
            areas.AddRange(await GetAllAdm2ByAdm1Async(governorate.Gid, ct));
        }

        return await MapManyAsync(areas, ct);
    }

    public async Task RefreshWeeklyAsync(CancellationToken ct = default)
    {
        var areas = await GetTargetAdm2AreasAsync(ct);
        var lookbackDays = Math.Max(1, options.NewsLookbackDays);
        var staleBefore = DateTimeOffset.UtcNow.AddHours(-Math.Max(1, options.WeeklyRefreshIntervalHours));

        logger.LogInformation(
            "Starting weekly safety index refresh for {AreaCount} adm2 areas with {LookbackDays} day lookback",
            areas.Count,
            lookbackDays);

        foreach (var area in areas)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var existing = await safetyIndexRepository.GetByAdm2GidAsync(area.Gid, ct);
                if (existing?.WeeklyCalculatedAtUtc is not null && existing.WeeklyCalculatedAtUtc > staleBefore)
                {
                    logger.LogInformation(
                        "Skipping adm2 {Adm2Gid}; weekly safety index is still fresh",
                        area.Gid);
                    continue;
                }

                await RefreshAreaWeeklyAsync(area, lookbackDays, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed weekly safety index refresh for adm2 {Adm2Gid}", area.Gid);
            }
        }

        logger.LogInformation("Finished weekly safety index refresh");
    }

    public async Task RunInitialHistoricalCollectionAsync(CancellationToken ct = default)
    {
        if (!options.RunInitialHistoricalCollectionOnStartup)
        {
            logger.LogInformation("Initial historical safety collection is disabled");
            return;
        }

        var areas = await GetTargetAdm2AreasAsync(ct);
        logger.LogInformation(
            "Starting initial historical safety collection for {AreaCount} adm2 areas",
            areas.Count);

        foreach (var area in areas)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var existing = await safetyIndexRepository.GetByAdm2GidAsync(area.Gid, ct);
                if (existing?.HistoricalCalculatedAtUtc is not null)
                {
                    logger.LogInformation(
                        "Skipping adm2 {Adm2Gid}; historical safety index already exists",
                        area.Gid);
                    continue;
                }

                await RefreshAreaHistoricalAsync(area, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed initial historical safety collection for adm2 {Adm2Gid}", area.Gid);
            }
        }

        logger.LogInformation("Finished initial historical safety collection");
    }

    private async Task RefreshAreaWeeklyAsync(Adm2 area, int lookbackDays, CancellationToken ct)
    {
        var areaNameAr = GetSearchName(area);
        logger.LogInformation("Refreshing weekly safety index for adm2 {Adm2Gid} ({AreaNameAr})", area.Gid, areaNameAr);

        var weeklyNews = await googleNewsRssClient.SearchAsync(areaNameAr, lookbackDays, ct);
        var history = await newsHistoryStore.MergeAsync(area.Gid, weeklyNews, ct);

        var weeklyEstimate = await safetyScoringClient.EstimateAsync(
            areaNameAr,
            weeklyNews.Select(x => x.Title).ToArray(),
            SafetyScorePeriod.Weekly,
            ct);

        var historicalEstimate = await safetyScoringClient.EstimateAsync(
            areaNameAr,
            history.Select(x => x.Title).ToArray(),
            SafetyScorePeriod.Historical,
            ct);

        var result = await safetyIndexRepository.GetByAdm2GidAsync(area.Gid, ct)
            ?? new SafetyIndexResult { Adm2Gid = area.Gid };

        var now = DateTimeOffset.UtcNow;
        if (weeklyEstimate is not null)
        {
            result.WeeklyScore = weeklyEstimate.Score;
            result.WeeklyGeneralSafetyDescription = weeklyEstimate.GeneralSafetyDescription;
            result.WeeklyTrendingEventDescription = weeklyEstimate.TrendingEventDescription;
            result.WeeklyCalculatedAtUtc = now;
        }

        result.WeeklyNewsFromUtc = now.AddDays(-lookbackDays);
        result.WeeklyNewsToUtc = now;
        result.WeeklyNewsItemCount = weeklyNews.Count;

        if (historicalEstimate is not null)
        {
            result.HistoricalScore = historicalEstimate.Score;
            result.HistoricalGeneralSafetyDescription = historicalEstimate.GeneralSafetyDescription;
            result.HistoricalTrendingEventDescription = historicalEstimate.TrendingEventDescription;
            result.HistoricalCalculatedAtUtc = now;
        }

        result.HistoricalNewsItemCount = history.Count;

        await safetyIndexRepository.UpsertAsync(result, ct);
        logger.LogInformation(
            "Updated safety index for adm2 {Adm2Gid}: weekly items {WeeklyCount}, historical items {HistoricalCount}",
            area.Gid,
            weeklyNews.Count,
            history.Count);
    }

    private async Task RefreshAreaHistoricalAsync(Adm2 area, CancellationToken ct)
    {
        var areaNameAr = GetSearchName(area);
        logger.LogInformation(
            "Collecting initial historical safety news for adm2 {Adm2Gid} ({AreaNameAr})",
            area.Gid,
            areaNameAr);

        var historicalNews = await googleNewsRssClient.SearchAsync(areaNameAr, lookbackDays: null, ct);
        var history = await newsHistoryStore.MergeAsync(area.Gid, historicalNews, ct);

        var historicalEstimate = await safetyScoringClient.EstimateAsync(
            areaNameAr,
            history.Select(x => x.Title).ToArray(),
            SafetyScorePeriod.Historical,
            ct);

        var result = await safetyIndexRepository.GetByAdm2GidAsync(area.Gid, ct)
            ?? new SafetyIndexResult { Adm2Gid = area.Gid };

        if (historicalEstimate is not null)
        {
            result.HistoricalScore = historicalEstimate.Score;
            result.HistoricalGeneralSafetyDescription = historicalEstimate.GeneralSafetyDescription;
            result.HistoricalTrendingEventDescription = historicalEstimate.TrendingEventDescription;
            result.HistoricalCalculatedAtUtc = DateTimeOffset.UtcNow;
        }

        result.HistoricalNewsItemCount = history.Count;

        await safetyIndexRepository.UpsertAsync(result, ct);
    }

    private async Task<List<Adm2SafetyIndexResponseDto>> MapManyAsync(List<Adm2> areas, CancellationToken ct)
    {
        var results = await safetyIndexRepository.GetByAdm2GidsAsync(areas.Select(x => x.Gid).ToArray(), ct);
        var resultsByAdm2 = results.ToDictionary(x => x.Adm2Gid);

        return areas
            .OrderBy(x => x.NameEn)
            .Select(x => Map(x, resultsByAdm2.GetValueOrDefault(x.Gid)))
            .ToList();
    }

    private async Task<List<Adm2>> GetTargetAdm2AreasAsync(CancellationToken ct)
    {
        var areas = await GetAllAdm2Async(ct);
        if (options.LimitAdm2Gids.Count == 0)
        {
            return areas;
        }

        var allowed = options.LimitAdm2Gids.ToHashSet();
        return areas.Where(x => allowed.Contains(x.Gid)).ToList();
    }

    private async Task<List<Adm2>> GetAllAdm2Async(CancellationToken ct)
    {
        var areas = new List<Adm2>();
        var page = 1;

        while (true)
        {
            var batch = await adm2Repository.GetAllAsync(new RegionListQuery
            {
                Page = page,
                PageSize = 500,
            }, ct);

            areas.AddRange(batch);
            if (batch.Count < 500)
            {
                break;
            }

            page++;
        }

        return areas;
    }

    private async Task<List<Adm1>> GetAllAdm1ByAdm0Async(int adm0Gid, CancellationToken ct)
    {
        var areas = new List<Adm1>();
        var page = 1;

        while (true)
        {
            var batch = await adm1Repository.GetByAdm0Async(adm0Gid, new RegionListQuery
            {
                Page = page,
                PageSize = 500,
            }, ct);

            areas.AddRange(batch);
            if (batch.Count < 500)
            {
                break;
            }

            page++;
        }

        return areas;
    }

    private async Task<List<Adm2>> GetAllAdm2ByAdm1Async(int adm1Gid, CancellationToken ct)
    {
        var areas = new List<Adm2>();
        var page = 1;

        while (true)
        {
            var batch = await adm2Repository.GetByAdm1Async(adm1Gid, new RegionListQuery
            {
                Page = page,
                PageSize = 500,
            }, ct);

            areas.AddRange(batch);
            if (batch.Count < 500)
            {
                break;
            }

            page++;
        }

        return areas;
    }

    private static string GetSearchName(Adm2 area)
        => !string.IsNullOrWhiteSpace(area.NameAr) ? area.NameAr : area.NameEn;

    private static Adm2SafetyIndexResponseDto Map(Adm2 area, SafetyIndexResult? result)
        => new()
        {
            Adm2Gid = area.Gid,
            Adm1Gid = area.Adm1Gid,
            NameEn = area.NameEn,
            NameAr = area.NameAr,
            WeeklyScore = new SafetyScoreDto
            {
                Score = result?.WeeklyScore,
                GeneralSafetyDescription = result?.WeeklyGeneralSafetyDescription,
                TrendingEventDescription = result?.WeeklyTrendingEventDescription,
                CalculatedAtUtc = result?.WeeklyCalculatedAtUtc,
                NewsItemCount = result?.WeeklyNewsItemCount ?? 0,
            },
            HistoricalScore = new SafetyScoreDto
            {
                Score = result?.HistoricalScore,
                GeneralSafetyDescription = result?.HistoricalGeneralSafetyDescription,
                TrendingEventDescription = result?.HistoricalTrendingEventDescription,
                CalculatedAtUtc = result?.HistoricalCalculatedAtUtc,
                NewsItemCount = result?.HistoricalNewsItemCount ?? 0,
            },
        };
}
