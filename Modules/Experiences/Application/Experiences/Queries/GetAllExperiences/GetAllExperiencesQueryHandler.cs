using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetAllExperiences;

public class GetAllExperiencesQueryHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;

    public GetAllExperiencesQueryHandler(
        IExperienceRepository experienceRepository,
        Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService regionReferenceService)
    {
        _experienceRepository = experienceRepository;
        _regionReferenceService = regionReferenceService;
    }

    public async Task<List<ExperienceSummaryDto>> HandleAsync(
        GetAllExperiencesQuery query,
        CancellationToken cancellationToken = default)
    {
        Glinter.Modules.Experiences.Application.Common.ExperiencePagination.Validate(
            query.Page,
            query.PageSize);

        if (query.Adm3Gid.HasValue && query.Adm3Gid.Value <= 0)
            throw new ValidationException("Adm3Gid must be greater than zero.");

        if (query.CategoryId == Guid.Empty)
            throw new ValidationException("CategoryId must be a valid id.");

        if (query.VibeId == Guid.Empty)
            throw new ValidationException("VibeId must be a valid id.");

        if (!string.IsNullOrWhiteSpace(query.Tag) && query.Tag.Trim().Length > 100)
            throw new ValidationException("Tag cannot exceed 100 characters.");

        if (query.MinPrice.HasValue && query.MinPrice.Value < 0)
        {
            throw new ValidationException("MinPrice cannot be negative.");
        }

        if (query.MaxPrice.HasValue && query.MaxPrice.Value < 0)
        {
            throw new ValidationException("MaxPrice cannot be negative.");
        }

        if (query.MinPrice.HasValue &&
            query.MaxPrice.HasValue &&
            query.MinPrice.Value > query.MaxPrice.Value)
        {
            throw new ValidationException("MinPrice cannot be greater than MaxPrice.");
        }

        if (query.Guests.HasValue && query.Guests.Value <= 0)
        {
            throw new ValidationException("Guests must be greater than zero.");
        }

        if (!string.IsNullOrWhiteSpace(query.Search) && query.Search.Trim().Length > 200)
            throw new ValidationException("Search cannot exceed 200 characters.");

        if (!string.IsNullOrWhiteSpace(query.Currency) &&
            query.Currency.Trim().Length > 10)
            throw new ValidationException("Currency cannot exceed 10 characters.");

        if (query.MinDurationMinutes is <= 0 || query.MaxDurationMinutes is <= 0)
            throw new ValidationException("Duration filters must be greater than zero.");

        if (query.MinDurationMinutes.HasValue &&
            query.MaxDurationMinutes.HasValue &&
            query.MinDurationMinutes > query.MaxDurationMinutes)
        {
            throw new ValidationException(
                "MinDurationMinutes cannot be greater than MaxDurationMinutes.");
        }

        if (query.AvailableFromUtc.HasValue != query.AvailableToUtc.HasValue)
            throw new ValidationException(
                "AvailableFromUtc and AvailableToUtc must be provided together.");

        var availableFromUtc = NormalizeUtc(
            query.AvailableFromUtc,
            nameof(query.AvailableFromUtc));
        var availableToUtc = NormalizeUtc(
            query.AvailableToUtc,
            nameof(query.AvailableToUtc));

        if (availableFromUtc.HasValue &&
            availableToUtc <= availableFromUtc)
            throw new ValidationException("AvailableToUtc must be after AvailableFromUtc.");

        var sortBy = string.IsNullOrWhiteSpace(query.SortBy)
            ? "newest"
            : query.SortBy.Trim().ToLowerInvariant();
        if (sortBy is not ("newest" or "price_asc" or "price_desc" or "duration_asc"))
        {
            throw new ValidationException(
                "SortBy must be newest, price_asc, price_desc, or duration_asc.");
        }

        var experiences = await _experienceRepository.GetFilteredAsync(
            query.Adm3Gid,
            query.CategoryId,
            query.MinPrice,
            query.MaxPrice,
            query.Guests,
            query.VibeId,
            query.Tag,
            query.Search,
            query.Currency,
            query.MinDurationMinutes,
            query.MaxDurationMinutes,
            availableFromUtc,
            availableToUtc,
            sortBy,
            query.Page,
            query.PageSize,
            cancellationToken);

        var regions = await _regionReferenceService.GetNeighbourhoodsAsync(
            experiences.Select(x => x.Adm3Gid),
            cancellationToken);

        return experiences
            .Select(experience => ExperiencesMappings.ToExperienceSummary(
                experience,
                regions.GetValueOrDefault(experience.Adm3Gid)))
            .ToList();
    }

    private static DateTime? NormalizeUtc(DateTime? value, string fieldName)
    {
        if (!value.HasValue)
            return null;

        if (value.Value.Kind == DateTimeKind.Unspecified)
        {
            throw new ValidationException(
                $"{fieldName} must include Z or an explicit UTC offset.");
        }

        return value.Value.Kind == DateTimeKind.Utc
            ? value.Value
            : value.Value.ToUniversalTime();
    }
}
