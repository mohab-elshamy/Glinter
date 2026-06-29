using System.Globalization;
using System.Text;
using System.Text.Json;
using Glinter.Modules.Experiences.Application.Dtos;
using Glinter.Modules.Experiences.Domain.Entities;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Regions.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Application.Services;

public class ExperienceService
{
    private readonly ExperiencesDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProfilesReadService _profilesReadService;
    private readonly IRegionsPointLookupRepository _regionsPointLookupRepository;

    public ExperienceService(
        ExperiencesDbContext dbContext,
        ICurrentUserService currentUserService,
        IProfilesReadService profilesReadService,
        IRegionsPointLookupRepository regionsPointLookupRepository)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _profilesReadService = profilesReadService;
        _regionsPointLookupRepository = regionsPointLookupRepository;
    }

    public async Task<ExperienceResponse> CreateProviderExperienceAsync(
        CreateExperienceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Experience name is required.");
        }

        var userId = _currentUserService.UserId
            ?? throw new InvalidOperationException("Authenticated user id is missing.");

        Guid? providerProfileId = null;

        if (_currentUserService.Roles.Contains(RoleNames.ExperienceProvider))
        {
            providerProfileId = await _profilesReadService.GetExperienceProviderProfileIdByUserIdAsync(
                userId,
                cancellationToken);
        }

        var experience = new Experience
        {
            Category = request.Category,
            SourceType = ExperienceSourceType.Provider,
            CreatedByUserId = userId,
            ProviderProfileId = providerProfileId,
            Name = CleanText(request.Name) ?? string.Empty,
            Description = CleanText(request.Description),
            Address = CleanText(request.Address),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            GoogleMapsLink = NormalizeString(request.GoogleMapsLink),
            PhoneInternational = NormalizeString(request.PhoneInternational),
            PriceRange = NormalizeString(request.PriceRange),
            Website = NormalizeString(request.Website)
        };

        await AttachRegionHierarchyAsync(experience, cancellationToken);
        AddProviderChildren(experience, request);

        _dbContext.Experiences.Add(experience);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(experience, DateTime.Now);
    }

    public async Task<ImportExperiencesResponse> ImportThirdPartyAsync(
        ExperienceCategory category,
        IReadOnlyCollection<JsonElement> items,
        CancellationToken cancellationToken)
    {
        var response = new ImportExperiencesResponse();

        foreach (var item in items)
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                response.Skipped++;
                continue;
            }

            var name = CleanText(GetString(item, "name"));

            if (string.IsNullOrWhiteSpace(name))
            {
                response.Skipped++;
                continue;
            }

            var cid = NormalizeString(GetString(item, "cid"));
            var latitude = GetNestedDouble(item, "coordinates", "latitude");
            var longitude = GetNestedDouble(item, "coordinates", "longitude");
            var address = CleanText(GetString(item, "address"));

            var existing = await FindExistingImportedExperienceAsync(
                cid,
                name,
                address,
                latitude,
                longitude,
                cancellationToken);

            var isNew = existing is null;
            var experience = existing ?? new Experience
            {
                SourceType = ExperienceSourceType.ThirdParty,
                CreatedAtUtc = DateTime.UtcNow
            };

            experience.Category = category;
            experience.Name = name;
            experience.Description = CleanText(GetString(item, "description"));
            experience.Address = address;
            experience.Cid = cid;
            experience.Latitude = latitude;
            experience.Longitude = longitude;
            experience.GoogleMapsLink = NormalizeString(GetString(item, "link"));
            experience.PhoneInternational = NormalizeString(GetString(item, "phone_international"));
            experience.PriceRange = NormalizeString(GetString(item, "price_range"));
            experience.Reviews = GetInt(item, "reviews");
            experience.Rating = GetDecimal(item, "rating");
            experience.Website = NormalizeString(GetString(item, "website"));
            experience.UpdatedAtUtc = isNew ? null : DateTime.UtcNow;

            await AttachRegionHierarchyAsync(experience, cancellationToken);

            if (isNew)
            {
                _dbContext.Experiences.Add(experience);
                response.Created++;
            }
            else
            {
                response.Updated++;
                ClearChildCollections(experience);
            }

            AddImportedChildren(experience, item);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<PagedResponse<ExperienceResponse>> GetExperiencesAsync(
        ExperienceListRequest request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = ApplyFilters(_dbContext.Experiences.AsNoTracking(), request);

        var totalCount = await query.CountAsync(cancellationToken);
        var experiences = await IncludeResponseData(query)
            .OrderByDescending(x => x.Rating)
            .ThenByDescending(x => x.Reviews)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var now = DateTime.Now;

        return new PagedResponse<ExperienceResponse>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = experiences.Select(x => ToResponse(x, now)).ToList()
        };
    }

    public async Task<List<ExperienceMapItemResponse>> GetMapItemsAsync(
        ExperienceListRequest request,
        CancellationToken cancellationToken)
    {
        var experiences = await ApplyFilters(_dbContext.Experiences.AsNoTracking(), request)
            .Include(x => x.FeaturedImages)
            .Include(x => x.Hours)
            .Include(x => x.PopularTimes)
            .Where(x => x.Latitude != null && x.Longitude != null)
            .OrderByDescending(x => x.Rating)
            .ThenByDescending(x => x.Reviews)
            .Take(1000)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var now = DateTime.Now;

        return experiences
            .Select(x =>
            {
                var insight = BuildVisitInsight(x, now);

                return new ExperienceMapItemResponse
                {
                    Id = x.Id,
                    Category = x.Category,
                    SourceType = x.SourceType,
                    Name = x.Name,
                    Address = x.Address,
                    Adm0Gid = x.Adm0Gid,
                    Adm1Gid = x.Adm1Gid,
                    Adm2Gid = x.Adm2Gid,
                    Adm3Gid = x.Adm3Gid,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    Rating = x.Rating,
                    Reviews = x.Reviews,
                    PrimaryImage = x.FeaturedImages.OrderBy(i => i.Id).FirstOrDefault()?.Link,
                    IsOpenNow = insight.IsOpen,
                    PopularityPercentageNow = insight.PopularityPercentage
                };
            })
            .ToList();
    }

    public async Task<ExperienceResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var experience = await IncludeResponseData(_dbContext.Experiences.AsNoTracking())
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return experience is null ? null : ToResponse(experience, DateTime.Now);
    }

    public async Task<List<ExperienceReviewResponse>?> GetReviewsAsync(
        int experienceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Experiences
            .AsNoTracking()
            .AnyAsync(x => x.Id == experienceId, cancellationToken);

        if (!exists)
        {
            return null;
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        return await _dbContext.ExperienceReviews
            .AsNoTracking()
            .Where(x => x.ExperienceId == experienceId)
            .OrderByDescending(x => x.PublishedAtDate)
            .ThenByDescending(x => x.Rating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ExperienceReviewResponse
            {
                Id = x.Id,
                ExternalReviewId = x.ExternalReviewId,
                ReviewerName = x.ReviewerName,
                Rating = x.Rating,
                ReviewText = x.ReviewText,
                PublishedAtDate = x.PublishedAtDate,
                SourceList = x.SourceList
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ExperienceReviewsForLlmResponse?> GetReviewsForLlmAsync(
        int experienceId,
        CancellationToken cancellationToken)
    {
        var experience = await _dbContext.Experiences
            .AsNoTracking()
            .Where(x => x.Id == experienceId)
            .Select(x => new { x.Id, x.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (experience is null)
        {
            return null;
        }

        var reviews = await _dbContext.ExperienceReviews
            .AsNoTracking()
            .Where(x => x.ExperienceId == experienceId && !string.IsNullOrWhiteSpace(x.ReviewText))
            .OrderByDescending(x => x.PublishedAtDate)
            .ThenByDescending(x => x.Rating)
            .Select(x => new
            {
                x.ReviewerName,
                x.Rating,
                x.ReviewText
            })
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder();

        for (var index = 0; index < reviews.Count; index++)
        {
            var review = reviews[index];
            builder.Append(index + 1);
            builder.Append(". ");

            if (!string.IsNullOrWhiteSpace(review.ReviewerName))
            {
                builder.Append(review.ReviewerName);
                builder.Append(": ");
            }

            if (review.Rating is not null)
            {
                builder.Append('[');
                builder.Append(review.Rating);
                builder.Append("/5] ");
            }

            builder.AppendLine(review.ReviewText);
            builder.AppendLine();
        }

        return new ExperienceReviewsForLlmResponse
        {
            ExperienceId = experience.Id,
            ExperienceName = experience.Name,
            ReviewsCount = reviews.Count,
            ReviewsText = builder.ToString().Trim()
        };
    }

    public async Task<VisitInsightResponse?> GetVisitInsightAsync(
        int experienceId,
        DateTime visitAt,
        CancellationToken cancellationToken)
    {
        var experience = await _dbContext.Experiences
            .AsNoTracking()
            .Include(x => x.Hours)
            .Include(x => x.PopularTimes)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == experienceId, cancellationToken);

        return experience is null ? null : BuildVisitInsight(experience, visitAt);
    }

    private static IQueryable<Experience> IncludeResponseData(IQueryable<Experience> query)
    {
        return query
            .Include(x => x.FeaturedImages)
            .Include(x => x.Hours)
            .Include(x => x.PopularTimes)
            .Include(x => x.ReviewsPerRatings)
            .Include(x => x.Amenities)
            .Include(x => x.ExperienceReviews);
    }

    private IQueryable<Experience> ApplyFilters(
        IQueryable<Experience> query,
        ExperienceListRequest request)
    {
        if (request.Category is not null)
        {
            query = query.Where(x => x.Category == request.Category);
        }

        if (request.SourceType is not null)
        {
            query = query.Where(x => x.SourceType == request.SourceType);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = $"%{request.Search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, search) ||
                (x.Description != null && EF.Functions.ILike(x.Description, search)) ||
                (x.Address != null && EF.Functions.ILike(x.Address, search)));
        }

        if (request.MinRating is not null)
        {
            query = query.Where(x => x.Rating >= request.MinRating);
        }

        if (request.Adm0Gid is not null)
        {
            query = query.Where(x => x.Adm0Gid == request.Adm0Gid);
        }

        if (request.Adm1Gid is not null)
        {
            query = query.Where(x => x.Adm1Gid == request.Adm1Gid);
        }

        if (request.Adm2Gid is not null)
        {
            query = query.Where(x => x.Adm2Gid == request.Adm2Gid);
        }

        if (request.Adm3Gid is not null)
        {
            query = query.Where(x => x.Adm3Gid == request.Adm3Gid);
        }

        return query;
    }

    private async Task<Experience?> FindExistingImportedExperienceAsync(
        string? cid,
        string name,
        string? address,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken)
    {
        var query = IncludeResponseData(_dbContext.Experiences)
            .AsSplitQuery()
            .Where(x => x.SourceType == ExperienceSourceType.ThirdParty);

        if (!string.IsNullOrWhiteSpace(cid))
        {
            return await query.FirstOrDefaultAsync(x => x.Cid == cid, cancellationToken);
        }

        return await query.FirstOrDefaultAsync(
            x => x.Name == name &&
                 x.Address == address &&
                 x.Latitude == latitude &&
                 x.Longitude == longitude,
            cancellationToken);
    }

    private void ClearChildCollections(Experience experience)
    {
        _dbContext.ExperienceFeaturedImages.RemoveRange(experience.FeaturedImages);
        _dbContext.ExperienceHours.RemoveRange(experience.Hours);
        _dbContext.ExperiencePopularTimes.RemoveRange(experience.PopularTimes);
        _dbContext.ExperienceReviewsPerRatings.RemoveRange(experience.ReviewsPerRatings);
        _dbContext.ExperienceAmenities.RemoveRange(experience.Amenities);
        _dbContext.ExperienceReviews.RemoveRange(experience.ExperienceReviews);
    }

    private static ExperienceResponse ToResponse(Experience experience, DateTime now)
    {
        return new ExperienceResponse
        {
            Id = experience.Id,
            Category = experience.Category,
            SourceType = experience.SourceType,
            CreatedByUserId = experience.CreatedByUserId,
            ProviderProfileId = experience.ProviderProfileId,
            Name = experience.Name,
            Description = experience.Description,
            Address = experience.Address,
            Cid = experience.Cid,
            Adm0Gid = experience.Adm0Gid,
            Adm1Gid = experience.Adm1Gid,
            Adm2Gid = experience.Adm2Gid,
            Adm3Gid = experience.Adm3Gid,
            Latitude = experience.Latitude,
            Longitude = experience.Longitude,
            FeaturedImages = experience.FeaturedImages
                .OrderBy(x => x.Id)
                .Select(x => new ExperienceFeaturedImageResponse { Id = x.Id, Link = x.Link })
                .ToList(),
            Hours = experience.Hours
                .OrderBy(x => x.DayOfWeek)
                .ThenBy(x => x.OpensAt)
                .Select(x => new ExperienceHourResponse
                {
                    Id = x.Id,
                    DayOfWeek = x.DayOfWeek,
                    OpensAt = x.OpensAt,
                    ClosesAt = x.ClosesAt
                })
                .ToList(),
            GoogleMapsLink = experience.GoogleMapsLink,
            PopularTimes = experience.PopularTimes
                .OrderBy(x => x.DayOfWeek)
                .ThenBy(x => x.HourOfDay)
                .Select(x => new ExperiencePopularTimeResponse
                {
                    Id = x.Id,
                    DayOfWeek = x.DayOfWeek,
                    HourOfDay = x.HourOfDay,
                    PopularityPercentage = x.PopularityPercentage
                })
                .ToList(),
            PhoneInternational = experience.PhoneInternational,
            PriceRange = experience.PriceRange,
            Reviews = experience.Reviews,
            Rating = experience.Rating,
            ReviewsPerRating = experience.ReviewsPerRatings
                .OrderBy(x => x.Rating)
                .Select(x => new ExperienceReviewsPerRatingResponse
                {
                    Rating = x.Rating,
                    ReviewsCount = x.ReviewsCount
                })
                .ToList(),
            Website = experience.Website,
            Amenities = experience.Amenities
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToList(),
            FeaturedReviews = experience.ExperienceReviews
                .OrderByDescending(x => x.PublishedAtDate)
                .Take(10)
                .Select(ToReviewResponse)
                .ToList(),
            CurrentInsight = BuildVisitInsight(experience, now)
        };
    }

    private static ExperienceReviewResponse ToReviewResponse(ExperienceReview review)
    {
        return new ExperienceReviewResponse
        {
            Id = review.Id,
            ExternalReviewId = review.ExternalReviewId,
            ReviewerName = review.ReviewerName,
            Rating = review.Rating,
            ReviewText = review.ReviewText,
            PublishedAtDate = review.PublishedAtDate,
            SourceList = review.SourceList
        };
    }

    private static void AddProviderChildren(Experience experience, CreateExperienceRequest request)
    {
        AddImageLinks(experience, request.FeaturedImageLinks);

        foreach (var hour in request.Hours)
        {
            experience.Hours.Add(new ExperienceHour
            {
                DayOfWeek = hour.DayOfWeek,
                OpensAt = hour.OpensAt,
                ClosesAt = hour.ClosesAt
            });
        }

        foreach (var popularTime in request.PopularTimes)
        {
            if (popularTime.HourOfDay is < 0 or > 23)
            {
                continue;
            }

            experience.PopularTimes.Add(new ExperiencePopularTime
            {
                DayOfWeek = popularTime.DayOfWeek,
                HourOfDay = popularTime.HourOfDay,
                PopularityPercentage = Math.Clamp(popularTime.PopularityPercentage, 0, 100)
            });
        }

        AddAmenities(experience, request.Amenities);
    }

    private static void AddImportedChildren(Experience experience, JsonElement item)
    {
        AddImageLinks(experience, GetFeaturedImageLinks(item));
        AddHours(experience, item);
        AddPopularTimes(experience, item);
        AddReviewsPerRating(experience, item);
        AddAmenities(experience, GetAmenityNames(item));
        AddReviews(experience, item, "featured_reviews");
        AddReviews(experience, item, "detailed_reviews");
    }

    private static void AddImageLinks(Experience experience, IEnumerable<string?> links)
    {
        foreach (var link in links.Select(NormalizeString).Where(x => x is not null).Distinct())
        {
            experience.FeaturedImages.Add(new ExperienceFeaturedImage { Link = link! });
        }
    }

    private static List<string?> GetFeaturedImageLinks(JsonElement item)
    {
        var links = new List<string?>();
        var featuredImages = GetProperty(item, "featured_images");

        if (featuredImages is not null && featuredImages.Value.ValueKind == JsonValueKind.Array)
        {
            links.AddRange(featuredImages.Value
                .EnumerateArray()
                .Select(x => GetString(x, "link")));
        }

        links.Add(GetString(item, "featured_image"));

        return links;
    }

    private static void AddHours(Experience experience, JsonElement item)
    {
        var hoursElement = GetProperty(item, "hours");

        if (hoursElement is null || hoursElement.Value.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var day in hoursElement.Value.EnumerateArray())
        {
            var dayOfWeek = ParseDayName(GetString(day, "day"));

            if (dayOfWeek is null)
            {
                continue;
            }

            var timesElement = GetProperty(day, "times");

            if (timesElement is null || timesElement.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var timeElement in timesElement.Value.EnumerateArray())
            {
                if (timeElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var range = ParseOpenRange(timeElement.GetString());

                if (range is null)
                {
                    continue;
                }

                experience.Hours.Add(new ExperienceHour
                {
                    DayOfWeek = dayOfWeek.Value,
                    OpensAt = range.Value.OpensAt,
                    ClosesAt = range.Value.ClosesAt
                });
            }
        }
    }

    private static void AddPopularTimes(Experience experience, JsonElement item)
    {
        var popularTimes = GetProperty(item, "popular_times");

        if (popularTimes is null || popularTimes.Value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var day in popularTimes.Value.EnumerateObject())
        {
            var dayOfWeek = ParseDayName(day.Name);

            if (dayOfWeek is null || day.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var slot in day.Value.EnumerateArray())
            {
                var hour = GetInt(slot, "hour_of_day");
                var percentage = GetInt(slot, "popularity_percentage");

                if (hour is null or < 0 or > 23 || percentage is null)
                {
                    continue;
                }

                experience.PopularTimes.Add(new ExperiencePopularTime
                {
                    DayOfWeek = dayOfWeek.Value,
                    HourOfDay = hour.Value,
                    PopularityPercentage = Math.Clamp(percentage.Value, 0, 100)
                });
            }
        }
    }

    private static void AddReviewsPerRating(Experience experience, JsonElement item)
    {
        var reviewsPerRating = GetProperty(item, "reviews_per_rating");

        if (reviewsPerRating is null || reviewsPerRating.Value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var rating in reviewsPerRating.Value.EnumerateObject())
        {
            if (!int.TryParse(rating.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ratingValue))
            {
                continue;
            }

            var reviewsCount = rating.Value.ValueKind == JsonValueKind.Number &&
                               rating.Value.TryGetInt32(out var count)
                ? count
                : 0;

            experience.ReviewsPerRatings.Add(new ExperienceReviewsPerRating
            {
                Rating = ratingValue,
                ReviewsCount = reviewsCount
            });
        }
    }

    private static List<string> GetAmenityNames(JsonElement item)
    {
        var about = GetProperty(item, "about");
        var names = new List<string>();

        if (about is null || about.Value.ValueKind != JsonValueKind.Array)
        {
            return names;
        }

        foreach (var section in about.Value.EnumerateArray())
        {
            var sectionId = NormalizeString(GetString(section, "id"));
            var sectionName = NormalizeString(GetString(section, "name"));

            if (!string.Equals(sectionId, "amenities", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(sectionName, "Amenities", StringComparison.OrdinalIgnoreCase) &&
                sectionName != "وسائل الراحة")
            {
                continue;
            }

            var options = GetProperty(section, "options");

            if (options is null || options.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in options.Value.EnumerateArray())
            {
                if (GetBool(option, "enabled") == false)
                {
                    continue;
                }

                var name = CleanText(GetString(option, "name"));

                if (!string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }

    private static void AddAmenities(Experience experience, IEnumerable<string?> names)
    {
        foreach (var name in names.Select(CleanText).Where(x => x is not null).Distinct())
        {
            experience.Amenities.Add(new ExperienceAmenity { Name = name! });
        }
    }

    private static void AddReviews(Experience experience, JsonElement item, string propertyName)
    {
        var reviewsElement = GetProperty(item, propertyName);

        if (reviewsElement is null || reviewsElement.Value.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var existingReviewIds = experience.ExperienceReviews
            .Where(x => !string.IsNullOrWhiteSpace(x.ExternalReviewId))
            .Select(x => x.ExternalReviewId!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var review in reviewsElement.Value.EnumerateArray())
        {
            if (review.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var reviewId = NormalizeString(GetString(review, "review_id"));

            if (!string.IsNullOrWhiteSpace(reviewId) && !existingReviewIds.Add(reviewId))
            {
                continue;
            }

            experience.ExperienceReviews.Add(new ExperienceReview
            {
                ExternalReviewId = reviewId,
                ReviewerName = CleanText(GetString(review, "name")),
                Rating = GetInt(review, "rating"),
                ReviewText = CleanText(GetString(review, "review_text")),
                PublishedAtDate = GetDateTime(review, "published_at_date"),
                SourceList = propertyName
            });
        }
    }

    private async Task AttachRegionHierarchyAsync(
        Experience experience,
        CancellationToken cancellationToken)
    {
        experience.Adm0Gid = null;
        experience.Adm1Gid = null;
        experience.Adm2Gid = null;
        experience.Adm3Gid = null;

        if (experience.Latitude is null || experience.Longitude is null)
        {
            return;
        }

        var hierarchy = await _regionsPointLookupRepository.GetHierarchyByPointAsync(
            experience.Latitude.Value,
            experience.Longitude.Value,
            cancellationToken);

        experience.Adm0Gid = hierarchy?.Adm0Gid;
        experience.Adm1Gid = hierarchy?.Adm1Gid;
        experience.Adm2Gid = hierarchy?.Adm2Gid;
        experience.Adm3Gid = hierarchy?.Adm3Gid;
    }

    private static VisitInsightResponse BuildVisitInsight(Experience experience, DateTime visitAt)
    {
        var hour = visitAt.Hour;
        var openWindow = experience.Hours
            .Where(x => x.DayOfWeek == visitAt.DayOfWeek)
            .OrderBy(x => x.OpensAt)
            .FirstOrDefault(x => IsOpenAt(x, TimeOnly.FromTimeSpan(visitAt.TimeOfDay)));
        var hasHoursForDay = experience.Hours.Any(x => x.DayOfWeek == visitAt.DayOfWeek);
        var popularity = experience.PopularTimes.FirstOrDefault(x =>
            x.DayOfWeek == visitAt.DayOfWeek &&
            x.HourOfDay == hour);

        return new VisitInsightResponse
        {
            RequestedAt = visitAt,
            DayOfWeek = visitAt.DayOfWeek.ToString(),
            HourOfDay = hour,
            IsOpen = openWindow is not null ? true : hasHoursForDay ? false : null,
            OpenStatus = openWindow is not null ? "open" : hasHoursForDay ? "closed" : "unknown",
            PopularityPercentage = popularity?.PopularityPercentage,
            CrowdLevel = GetCrowdLevel(popularity?.PopularityPercentage),
            BestKnownOpenWindow = openWindow is null ? null : $"{openWindow.OpensAt:HH:mm}-{openWindow.ClosesAt:HH:mm}"
        };
    }

    private static bool IsOpenAt(ExperienceHour hour, TimeOnly time)
    {
        return hour.OpensAt <= hour.ClosesAt
            ? time >= hour.OpensAt && time < hour.ClosesAt
            : time >= hour.OpensAt || time < hour.ClosesAt;
    }

    private static OpenRange? ParseOpenRange(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Contains("مغلق", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("closed", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (value.Contains("24", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("مدار", StringComparison.OrdinalIgnoreCase))
        {
            return new OpenRange(new TimeOnly(0, 0), new TimeOnly(23, 59, 59));
        }

        var parts = value.Split('–', '-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 2)
        {
            return null;
        }

        var opensAt = ParseArabicTime(parts[0]);
        var closesAt = ParseArabicTime(parts[1]);

        return opensAt is null || closesAt is null
            ? null
            : new OpenRange(opensAt.Value, closesAt.Value);
    }

    private static TimeOnly? ParseArabicTime(string value)
    {
        var normalized = NormalizeDigits(value)
            .Replace("\u202f", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();

        var isPm = normalized.Contains('م') ||
                   normalized.Contains("PM", StringComparison.OrdinalIgnoreCase);
        var isAm = normalized.Contains('ص') ||
                   normalized.Contains("AM", StringComparison.OrdinalIgnoreCase);

        normalized = normalized
            .Replace("ص", string.Empty, StringComparison.Ordinal)
            .Replace("م", string.Empty, StringComparison.Ordinal)
            .Replace("AM", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("PM", string.Empty, StringComparison.OrdinalIgnoreCase);

        var components = normalized.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (components.Length == 0 || !int.TryParse(components[0], out var hour))
        {
            return null;
        }

        var minute = 0;

        if (components.Length > 1)
        {
            _ = int.TryParse(components[1], out minute);
        }

        if (isPm && hour < 12)
        {
            hour += 12;
        }
        else if (isAm && hour == 12)
        {
            hour = 0;
        }

        return hour is < 0 or > 23 || minute is < 0 or > 59
            ? null
            : new TimeOnly(hour, minute);
    }

    private static string NormalizeDigits(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(character switch
            {
                >= '\u0660' and <= '\u0669' => (char)('0' + character - '\u0660'),
                >= '\u06F0' and <= '\u06F9' => (char)('0' + character - '\u06F0'),
                _ => character
            });
        }

        return builder.ToString();
    }

    private static DayOfWeek? ParseDayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Enum.TryParse<DayOfWeek>(value, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return value.Trim() switch
        {
            "الأحد" => DayOfWeek.Sunday,
            "الاثنين" => DayOfWeek.Monday,
            "الثلاثاء" => DayOfWeek.Tuesday,
            "الأربعاء" => DayOfWeek.Wednesday,
            "الخميس" => DayOfWeek.Thursday,
            "الجمعة" => DayOfWeek.Friday,
            "السبت" => DayOfWeek.Saturday,
            _ => null
        };
    }

    private static string GetCrowdLevel(int? popularityPercentage)
    {
        return popularityPercentage switch
        {
            null => "unknown",
            < 20 => "quiet",
            < 50 => "moderate",
            < 75 => "busy",
            _ => "very_busy"
        };
    }

    private static JsonElement? GetProperty(JsonElement item, string propertyName)
    {
        return item.ValueKind == JsonValueKind.Object && item.TryGetProperty(propertyName, out var property)
            ? property
            : null;
    }

    private static string? GetString(JsonElement item, string propertyName)
    {
        var property = GetProperty(item, propertyName);

        if (property is null || property.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return property.Value.ValueKind == JsonValueKind.String
            ? property.Value.GetString()
            : property.Value.GetRawText();
    }

    private static int? GetInt(JsonElement item, string propertyName)
    {
        var property = GetProperty(item, propertyName);

        if (property is null || property.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out var number))
        {
            return number;
        }

        return int.TryParse(property.Value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
            ? number
            : null;
    }

    private static decimal? GetDecimal(JsonElement item, string propertyName)
    {
        var property = GetProperty(item, propertyName);

        if (property is null || property.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDecimal(out var number))
        {
            return number;
        }

        return decimal.TryParse(property.Value.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out number)
            ? number
            : null;
    }

    private static bool? GetBool(JsonElement item, string propertyName)
    {
        var property = GetProperty(item, propertyName);

        if (property is null || property.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return property.Value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => bool.TryParse(property.Value.ToString(), out var value) ? value : null
        };
    }

    private static DateTime? GetDateTime(JsonElement item, string propertyName)
    {
        var value = GetString(item, propertyName);

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
            : null;
    }

    private static double? GetNestedDouble(JsonElement item, string objectName, string propertyName)
    {
        var nested = GetProperty(item, objectName);

        if (nested is null)
        {
            return null;
        }

        var value = GetString(nested.Value, propertyName);

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static string? NormalizeString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? CleanText(string? value)
    {
        var normalized = NormalizeString(value);

        if (normalized is null)
        {
            return null;
        }

        return normalized
            .Replace("\\r\\n", "\n", StringComparison.Ordinal)
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\\r", "\r", StringComparison.Ordinal)
            .Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private readonly record struct OpenRange(TimeOnly OpensAt, TimeOnly ClosesAt);
}
