using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Domain.Entities;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Infrastructure.Services;

public sealed class ExperienceRecommendationReadService(
    ExperiencesDbContext dbContext) : IExperienceRecommendationReadService
{
    public async Task<IReadOnlyList<ExperienceRecommendationCandidate>> GetCandidatesAsync(
        ExperienceRecommendationQuery query,
        CancellationToken cancellationToken = default)
    {
        var categoryNames = query.CategoryNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var maxPerCategory = Math.Clamp(query.MaxPerCategory, 1, 5000);
        var result = new List<ExperienceRecommendationCandidate>(
            categoryNames.Length * Math.Min(maxPerCategory, 100));

        foreach (var categoryName in categoryNames)
        {
            if (!Enum.TryParse<ExperienceCategory>(
                    categoryName,
                    true,
                    out var category))
            {
                continue;
            }

            var categoryQuery = ApplyAdministrativeFilters(
                    dbContext.Experiences.AsNoTracking(),
                    query)
                .Where(x =>
                    x.Category == category &&
                    x.IsActive &&
                    x.ModerationStatus == ExperienceModerationStatus.Approved &&
                    x.Latitude != null &&
                    x.Longitude != null);

            var candidates = await categoryQuery
                .OrderByDescending(x => x.Rating)
                .ThenByDescending(x => x.Reviews)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id)
                .Take(maxPerCategory)
                .Select(x => new ExperienceRecommendationCandidate(
                    x.Id,
                    x.Category.ToString(),
                    x.Name,
                    x.Latitude!.Value,
                    x.Longitude!.Value,
                    x.Rating,
                    x.Reviews))
                .ToListAsync(cancellationToken);

            result.AddRange(candidates);
        }

        return result;
    }

    private static IQueryable<Experience> ApplyAdministrativeFilters(
        IQueryable<Experience> query,
        ExperienceRecommendationQuery request)
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
}
