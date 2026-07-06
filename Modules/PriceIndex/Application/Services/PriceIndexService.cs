using Glinter.Modules.Experiences.Domain.Entities;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.PriceIndex.Application.Abstractions;
using Glinter.Modules.PriceIndex.Application.Dtos;
using Glinter.Modules.Stays.Domain.Entities;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.PriceIndex.Application.Services;

public sealed class PriceIndexService(
    StaysDbContext staysDbContext,
    ExperiencesDbContext experiencesDbContext) : IPriceIndexService
{
    private const string Currency = "USD";
    private const int MaxBatchSize = 200;

    public async Task<PriceIndexResponseDto> GetAsync(
        PriceIndexRequest request,
        CancellationToken cancellationToken = default)
    {
        var filteredStayPrices = await GetStayPricesAsync(
            request,
            cancellationToken);
        var filteredExperiencePrices = await GetExperiencePricesAsync(
            request,
            cancellationToken);

        var baselineStayPrices = await GetStayPricesAsync(
            new PriceIndexRequest(),
            cancellationToken);
        var baselineExperiencePrices = await GetExperiencePricesAsync(
            new PriceIndexRequest(),
            cancellationToken);

        var filteredCombinedPrices = filteredStayPrices
            .Concat(filteredExperiencePrices)
            .ToList();
        var baselineCombinedPrices = baselineStayPrices
            .Concat(baselineExperiencePrices)
            .ToList();

        return new PriceIndexResponseDto(
            Currency,
            new PriceIndexFilterDto(
                request.Adm0Gid,
                request.Adm1Gid,
                request.Adm2Gid,
                request.Adm3Gid),
            BuildSegment("stays", filteredStayPrices, baselineStayPrices),
            BuildSegment(
                "experiences",
                filteredExperiencePrices,
                baselineExperiencePrices),
            BuildSegment(
                "combined",
                filteredCombinedPrices,
                baselineCombinedPrices));
    }

    public async Task<IReadOnlyList<PriceIndexResponseDto>> GetBatchAsync(
        IReadOnlyList<PriceIndexRequest> requests,
        CancellationToken cancellationToken = default)
    {
        var boundedRequests = (requests ?? [])
            .Take(MaxBatchSize)
            .ToArray();

        var results = new List<PriceIndexResponseDto>(boundedRequests.Length);
        foreach (var request in boundedRequests)
        {
            results.Add(await GetAsync(request, cancellationToken));
        }

        return results;
    }

    private async Task<List<decimal>> GetStayPricesAsync(
        PriceIndexRequest request,
        CancellationToken cancellationToken)
    {
        var query = ApplyStayFilters(
                staysDbContext.Stays.AsNoTracking(),
                request)
            .Where(x => x.IsActive && x.Price != null && x.Price >= 0);

        return await query
            .Select(x => x.Price!.Value)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<decimal>> GetExperiencePricesAsync(
        PriceIndexRequest request,
        CancellationToken cancellationToken)
    {
        var query = ApplyExperienceFilters(
                experiencesDbContext.Experiences.AsNoTracking(),
                request)
            .Where(x =>
                x.IsActive &&
                x.ModerationStatus == ExperienceModerationStatus.Approved &&
                (x.PriceRangeMin != null || x.PriceRangeMax != null));

        var ranges = await query
            .Select(x => new
            {
                x.PriceRangeMin,
                x.PriceRangeMax
            })
            .ToListAsync(cancellationToken);

        return ranges
            .Select(x => NormalizeExperiencePrice(
                x.PriceRangeMin,
                x.PriceRangeMax))
            .Where(x => x is >= 0)
            .Select(x => x!.Value)
            .ToList();
    }

    private static IQueryable<Stay> ApplyStayFilters(
        IQueryable<Stay> query,
        PriceIndexRequest request)
    {
        if (request.Adm0Gid is not null)
            query = query.Where(x => x.Adm0Gid == request.Adm0Gid);
        if (request.Adm1Gid is not null)
            query = query.Where(x => x.Adm1Gid == request.Adm1Gid);
        if (request.Adm2Gid is not null)
            query = query.Where(x => x.Adm2Gid == request.Adm2Gid);
        if (request.Adm3Gid is not null)
            query = query.Where(x => x.Adm3Gid == request.Adm3Gid);

        return query;
    }

    private static IQueryable<Experience> ApplyExperienceFilters(
        IQueryable<Experience> query,
        PriceIndexRequest request)
    {
        if (request.Adm0Gid is not null)
            query = query.Where(x => x.Adm0Gid == request.Adm0Gid);
        if (request.Adm1Gid is not null)
            query = query.Where(x => x.Adm1Gid == request.Adm1Gid);
        if (request.Adm2Gid is not null)
            query = query.Where(x => x.Adm2Gid == request.Adm2Gid);
        if (request.Adm3Gid is not null)
            query = query.Where(x => x.Adm3Gid == request.Adm3Gid);

        return query;
    }

    private static decimal? NormalizeExperiencePrice(
        int? priceRangeMin,
        int? priceRangeMax)
    {
        if (priceRangeMin is null && priceRangeMax is null)
            return null;

        var minimum = priceRangeMin ?? priceRangeMax!.Value;
        var maximum = priceRangeMax ?? priceRangeMin!.Value;
        return (Math.Min(minimum, maximum) + Math.Max(minimum, maximum)) / 2m;
    }

    private static PriceIndexSegmentDto BuildSegment(
        string segment,
        IReadOnlyCollection<decimal> prices,
        IReadOnlyCollection<decimal> baselinePrices)
    {
        var orderedPrices = prices.Order().ToArray();
        decimal? average = orderedPrices.Length == 0
            ? null
            : RoundMoney(orderedPrices.Average());
        decimal? baselineAverage = baselinePrices.Count == 0
            ? null
            : baselinePrices.Average();

        return new PriceIndexSegmentDto(
            segment,
            orderedPrices.Length,
            CalculateIndex(average, baselineAverage),
            orderedPrices.Length == 0 ? null : RoundMoney(orderedPrices.First()),
            orderedPrices.Length == 0 ? null : RoundMoney(orderedPrices.Last()),
            average,
            Percentile(orderedPrices, 0.50m),
            Percentile(orderedPrices, 0.25m),
            Percentile(orderedPrices, 0.75m));
    }

    private static decimal? CalculateIndex(
        decimal? average,
        decimal? baselineAverage)
    {
        if (average is null || baselineAverage is null || baselineAverage <= 0)
            return null;

        return Math.Round((average.Value / baselineAverage.Value) * 100m, 2);
    }

    private static decimal? Percentile(
        IReadOnlyList<decimal> orderedPrices,
        decimal percentile)
    {
        if (orderedPrices.Count == 0)
            return null;
        if (orderedPrices.Count == 1)
            return RoundMoney(orderedPrices[0]);

        var position = (orderedPrices.Count - 1) * percentile;
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);

        if (lowerIndex == upperIndex)
            return RoundMoney(orderedPrices[lowerIndex]);

        var weight = position - lowerIndex;
        var value =
            orderedPrices[lowerIndex] +
            (orderedPrices[upperIndex] - orderedPrices[lowerIndex]) * weight;

        return RoundMoney(value);
    }

    private static decimal RoundMoney(decimal value)
        => Math.Round(value, 2);
}
