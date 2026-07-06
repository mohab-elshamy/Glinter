using Glinter.Modules.ComfortIndex.Application.Abstractions;
using Glinter.Modules.ComfortIndex.Application.Dtos;
using Glinter.Modules.Experiences.Domain.Entities;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.Stays.Domain.Entities;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.ComfortIndex.Application.Services;

public sealed class ComfortIndexService(
    StaysDbContext staysDbContext,
    ExperiencesDbContext experiencesDbContext) : IComfortIndexService
{
    private const int MaxBatchSize = 200;

    public async Task<ComfortIndexResponseDto> GetAsync(
        ComfortIndexRequest request,
        CancellationToken cancellationToken = default)
    {
        var filteredStayScores = await GetStayScoresAsync(
            request,
            cancellationToken);
        var filteredExperienceScores = await GetExperienceScoresAsync(
            request,
            cancellationToken);

        var baselineStayScores = await GetStayScoresAsync(
            new ComfortIndexRequest(),
            cancellationToken);
        var baselineExperienceScores = await GetExperienceScoresAsync(
            new ComfortIndexRequest(),
            cancellationToken);

        var filteredCombinedScores = filteredStayScores
            .Concat(filteredExperienceScores)
            .ToList();
        var baselineCombinedScores = baselineStayScores
            .Concat(baselineExperienceScores)
            .ToList();

        return new ComfortIndexResponseDto(
            new ComfortIndexFilterDto(
                request.Adm0Gid,
                request.Adm1Gid,
                request.Adm2Gid,
                request.Adm3Gid),
            BuildSegment("stays", filteredStayScores, baselineStayScores),
            BuildSegment(
                "experiences",
                filteredExperienceScores,
                baselineExperienceScores),
            BuildSegment(
                "combined",
                filteredCombinedScores,
                baselineCombinedScores));
    }

    public async Task<IReadOnlyList<ComfortIndexResponseDto>> GetBatchAsync(
        IReadOnlyList<ComfortIndexRequest> requests,
        CancellationToken cancellationToken = default)
    {
        var boundedRequests = (requests ?? [])
            .Take(MaxBatchSize)
            .ToArray();

        var results = new List<ComfortIndexResponseDto>(boundedRequests.Length);
        foreach (var request in boundedRequests)
        {
            results.Add(await GetAsync(request, cancellationToken));
        }

        return results;
    }

    private async Task<List<decimal>> GetStayScoresAsync(
        ComfortIndexRequest request,
        CancellationToken cancellationToken)
    {
        var values = await ApplyStayFilters(
                staysDbContext.Stays.AsNoTracking(),
                request)
            .Where(x =>
                x.IsActive &&
                x.Rating != null &&
                x.Rating > 0 &&
                x.Reviews != null &&
                x.Reviews > 1)
            .Select(x => new
            {
                Rating = x.Rating!.Value,
                Reviews = x.Reviews!.Value
            })
            .ToListAsync(cancellationToken);

        return values
            .Select(x => CalculateComfortScore(x.Rating, x.Reviews))
            .ToList();
    }

    private async Task<List<decimal>> GetExperienceScoresAsync(
        ComfortIndexRequest request,
        CancellationToken cancellationToken)
    {
        var values = await ApplyExperienceFilters(
                experiencesDbContext.Experiences.AsNoTracking(),
                request)
            .Where(x =>
                x.IsActive &&
                x.ModerationStatus == ExperienceModerationStatus.Approved &&
                x.Rating != null &&
                x.Rating > 0 &&
                x.Reviews != null &&
                x.Reviews > 1)
            .Select(x => new
            {
                Rating = x.Rating!.Value,
                Reviews = x.Reviews!.Value
            })
            .ToListAsync(cancellationToken);

        return values
            .Select(x => CalculateComfortScore(x.Rating, x.Reviews))
            .ToList();
    }

    private static IQueryable<Stay> ApplyStayFilters(
        IQueryable<Stay> query,
        ComfortIndexRequest request)
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
        ComfortIndexRequest request)
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

    private static decimal CalculateComfortScore(
        decimal rating,
        int reviews)
        => Math.Round(rating * (decimal)Math.Log(reviews), 4);

    private static ComfortIndexSegmentDto BuildSegment(
        string segment,
        IReadOnlyCollection<decimal> scores,
        IReadOnlyCollection<decimal> baselineScores)
    {
        var orderedScores = scores.Order().ToArray();
        decimal? average = orderedScores.Length == 0
            ? null
            : RoundScore(orderedScores.Average());
        decimal? baselineAverage = baselineScores.Count == 0
            ? null
            : baselineScores.Average();

        return new ComfortIndexSegmentDto(
            segment,
            orderedScores.Length,
            CalculateIndex(average, baselineAverage),
            orderedScores.Length == 0 ? null : RoundScore(orderedScores.First()),
            orderedScores.Length == 0 ? null : RoundScore(orderedScores.Last()),
            average,
            Percentile(orderedScores, 0.50m),
            Percentile(orderedScores, 0.25m),
            Percentile(orderedScores, 0.75m));
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
        IReadOnlyList<decimal> orderedScores,
        decimal percentile)
    {
        if (orderedScores.Count == 0)
            return null;
        if (orderedScores.Count == 1)
            return RoundScore(orderedScores[0]);

        var position = (orderedScores.Count - 1) * percentile;
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);

        if (lowerIndex == upperIndex)
            return RoundScore(orderedScores[lowerIndex]);

        var weight = position - lowerIndex;
        var value =
            orderedScores[lowerIndex] +
            (orderedScores[upperIndex] - orderedScores[lowerIndex]) * weight;

        return RoundScore(value);
    }

    private static decimal RoundScore(decimal value)
        => Math.Round(value, 2);
}
