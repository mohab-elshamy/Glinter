using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Stays.Application.Dtos;
using Glinter.Modules.Stays.Domain.Entities;
using Glinter.Modules.Stays.Domain.Enums;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Stays.Application.Services;

public class StayService
{
    private readonly StaysDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProfilesReadService _profilesReadService;
    private readonly IRegionsPointLookupRepository _regionsPointLookupRepository;

    public StayService(
        StaysDbContext dbContext,
        ICurrentUserService currentUserService,
        IProfilesReadService profilesReadService,
        IRegionsPointLookupRepository regionsPointLookupRepository)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _profilesReadService = profilesReadService;
        _regionsPointLookupRepository = regionsPointLookupRepository;
    }

    public async Task<StayResponse> CreateOwnerStayAsync(
        CreateStayRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Stay name is required.");
        }

        var userId = _currentUserService.UserId
            ?? throw new InvalidOperationException("Authenticated user id is missing.");

        Guid? hotelOwnerProfileId = null;

        if (_currentUserService.Roles.Contains(RoleNames.HotelOwner))
        {
            hotelOwnerProfileId = await _profilesReadService.GetHotelOwnerProfileIdByUserIdAsync(
                userId,
                cancellationToken);
        }

        var stay = new Stay
        {
            SourceType = StaySourceType.HotelOwner,
            CreatedByUserId = userId,
            HotelOwnerProfileId = hotelOwnerProfileId,
            Name = CleanText(request.Name) ?? string.Empty,
            Price = request.Price,
            Description = CleanText(request.Description),
            GoogleMapsLink = NormalizeString(request.GoogleMapsLink),
            Website = NormalizeString(request.Website),
            PhoneInternational = NormalizeString(request.PhoneInternational),
            LocationSummaryDescription = CleanText(request.LocationSummaryDescription),
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        await AttachRegionHierarchyAsync(stay, cancellationToken);
        AddImages(stay, request.ImageLinks);
        AddAmenities(stay, request.Amenities);
        AddBookingPlatforms(stay, request.BookingPlatforms);

        _dbContext.Stays.Add(stay);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(stay);
    }

    public async Task<ImportStaysResponse> ImportThirdPartyAsync(
        IReadOnlyCollection<JsonElement> items,
        CancellationToken cancellationToken)
    {
        var response = new ImportStaysResponse();

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

            var existing = await FindExistingImportedStayAsync(
                cid,
                name,
                latitude,
                longitude,
                cancellationToken);

            var isNew = existing is null;
            var stay = existing ?? new Stay
            {
                SourceType = StaySourceType.ThirdParty,
                CreatedAtUtc = DateTime.UtcNow
            };

            stay.Name = name;
            stay.Price = ParseMoney(GetString(item, "price"));
            stay.Description = CleanText(GetString(item, "description"));
            stay.GoogleMapsLink = NormalizeString(GetString(item, "link"));
            stay.Reviews = GetInt(item, "reviews");
            stay.Rating = GetDecimal(item, "rating");
            stay.Website = NormalizeString(GetString(item, "website"));
            stay.PhoneInternational = NormalizeString(GetString(item, "phone_international"));
            stay.LocationSummaryDescription = CleanText(GetNestedString(item, "location_summary", "description"));
            stay.Cid = cid;
            stay.Latitude = latitude;
            stay.Longitude = longitude;
            stay.UpdatedAtUtc = isNew ? null : DateTime.UtcNow;

            await AttachRegionHierarchyAsync(stay, cancellationToken);

            if (isNew)
            {
                _dbContext.Stays.Add(stay);
                response.Created++;
            }
            else
            {
                response.Updated++;
                ClearImportedCollections(stay);
            }

            AddImportedChildren(stay, item);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<PagedResponse<StayResponse>> GetStaysAsync(
        StayListRequest request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = ApplyFilters(_dbContext.Stays.AsNoTracking(), request);

        var totalCount = await query.CountAsync(cancellationToken);
        var stays = await ApplySorting(IncludeResponseData(query), request)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new PagedResponse<StayResponse>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = stays.Select(ToResponse).ToList()
        };
    }

    public async Task<StayResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var stay = await IncludeResponseData(_dbContext.Stays.AsNoTracking())
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return stay is null ? null : ToResponse(stay);
    }

    public async Task<List<StayRegionStatsResponse>> GetRegionStatsAsync(
        StayRegionStatsRequest request,
        CancellationToken cancellationToken)
    {
        var query = ApplyStatsFilters(_dbContext.Stays.AsNoTracking(), request);
        var pricedQuery = query.Where(x => x.Price != null);
        var minPrice = await pricedQuery.MinAsync(x => (decimal?)x.Price, cancellationToken);
        var maxPrice = await pricedQuery.MaxAsync(x => (decimal?)x.Price, cancellationToken);

        var groupedQuery = request.GroupBy switch
        {
            StayRegionGroupBy.Adm0 => query.Select(x => new { RegionGid = x.Adm0Gid, x.Price }),
            StayRegionGroupBy.Adm1 => query.Select(x => new { RegionGid = x.Adm1Gid, x.Price }),
            StayRegionGroupBy.Adm2 => query.Select(x => new { RegionGid = x.Adm2Gid, x.Price }),
            _ => query.Select(x => new { RegionGid = x.Adm3Gid, x.Price })
        };

        var stats = await groupedQuery
            .Where(x => x.RegionGid != null)
            .GroupBy(x => x.RegionGid!.Value)
            .Select(x => new StayRegionStatsResponse
            {
                GroupBy = request.GroupBy,
                RegionGid = x.Key,
                HotelsCount = x.Count(),
                AveragePrice = x
                    .Where(y => y.Price != null)
                    .Average(y => y.Price)
            })
            .OrderByDescending(x => x.HotelsCount)
            .ThenBy(x => x.RegionGid)
            .ToListAsync(cancellationToken);

        foreach (var stat in stats)
        {
            stat.PricePercentage = CalculatePricePercentage(stat.AveragePrice, minPrice, maxPrice);
        }

        return stats;
    }

    private static decimal? CalculatePricePercentage(
        decimal? averagePrice,
        decimal? minPrice,
        decimal? maxPrice)
    {
        if (averagePrice is null || minPrice is null || maxPrice is null)
        {
            return null;
        }

        if (maxPrice == minPrice)
        {
            return 0;
        }

        var percentage = (averagePrice.Value - minPrice.Value) / (maxPrice.Value - minPrice.Value) * 100;
        return Math.Round(Math.Clamp(percentage, 0, 100), 2);
    }

    public async Task<List<StayReviewResponse>?> GetReviewsAsync(
        int stayId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Stays
            .AsNoTracking()
            .AnyAsync(x => x.Id == stayId, cancellationToken);

        if (!exists)
        {
            return null;
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        return await _dbContext.StayReviews
            .AsNoTracking()
            .Where(x => x.StayId == stayId)
            .OrderByDescending(x => x.PublishedAtDate ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.Rating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StayReviewResponse
            {
                Id = x.Id,
                ExternalReviewId = x.ExternalReviewId,
                ReviewerName = x.ReviewerName,
                Rating = x.Rating,
                ReviewText = x.ReviewText,
                Platform = x.Platform,
                PublishedAtDate = x.PublishedAtDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<StayReviewResponse?> CreateGlinterReviewAsync(
        int stayId,
        CreateStayReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5)
        {
            throw new ArgumentException("Rating must be between 1 and 5.");
        }

        if (string.IsNullOrWhiteSpace(request.ReviewText))
        {
            throw new ArgumentException("Review text is required.");
        }

        var stayExists = await _dbContext.Stays.AnyAsync(x => x.Id == stayId, cancellationToken);

        if (!stayExists)
        {
            return null;
        }

        var userId = _currentUserService.UserId
            ?? throw new InvalidOperationException("Authenticated user id is missing.");

        var review = new StayReview
        {
            StayId = stayId,
            CreatedByUserId = userId,
            ReviewerName = _currentUserService.Email,
            Rating = request.Rating,
            ReviewText = CleanText(request.ReviewText),
            Platform = "glinter",
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.StayReviews.Add(review);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToReviewResponse(review);
    }

    public async Task<StayReviewsForLlmResponse?> GetReviewsForLlmAsync(
        int stayId,
        CancellationToken cancellationToken)
    {
        var stay = await _dbContext.Stays
            .AsNoTracking()
            .Where(x => x.Id == stayId)
            .Select(x => new { x.Id, x.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (stay is null)
        {
            return null;
        }

        var reviews = await _dbContext.StayReviews
            .AsNoTracking()
            .Where(x => x.StayId == stayId && !string.IsNullOrWhiteSpace(x.ReviewText))
            .OrderByDescending(x => x.PublishedAtDate ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.Rating)
            .Select(x => new { x.Platform, x.ReviewerName, x.Rating, x.ReviewText })
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder();

        for (var index = 0; index < reviews.Count; index++)
        {
            var review = reviews[index];
            builder.Append(index + 1);
            builder.Append(". ");
            builder.Append('[');
            builder.Append(review.Platform);
            builder.Append("] ");

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

        return new StayReviewsForLlmResponse
        {
            StayId = stay.Id,
            StayName = stay.Name,
            ReviewsCount = reviews.Count,
            ReviewsText = builder.ToString().Trim()
        };
    }

    private static IQueryable<Stay> IncludeResponseData(IQueryable<Stay> query)
    {
        return query
            .Include(x => x.Images)
            .Include(x => x.Amenities)
            .Include(x => x.ReviewsPerRatings)
            .Include(x => x.BookingPlatforms)
            .Include(x => x.StayReviews);
    }

    private IQueryable<Stay> ApplyFilters(IQueryable<Stay> query, StayListRequest request)
    {
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
                (x.LocationSummaryDescription != null && EF.Functions.ILike(x.LocationSummaryDescription, search)));
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

        if (request.MinPrice is not null)
        {
            query = query.Where(x => x.Price >= request.MinPrice);
        }

        if (request.MaxPrice is not null)
        {
            query = query.Where(x => x.Price <= request.MaxPrice);
        }

        if (request.MinRating is not null)
        {
            query = query.Where(x => x.Rating >= request.MinRating);
        }

        return query;
    }

    private IQueryable<Stay> ApplyStatsFilters(IQueryable<Stay> query, StayRegionStatsRequest request)
    {
        if (request.SourceType is not null)
        {
            query = query.Where(x => x.SourceType == request.SourceType);
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

        if (request.MinPrice is not null)
        {
            query = query.Where(x => x.Price >= request.MinPrice);
        }

        if (request.MaxPrice is not null)
        {
            query = query.Where(x => x.Price <= request.MaxPrice);
        }

        if (request.MinRating is not null)
        {
            query = query.Where(x => x.Rating >= request.MinRating);
        }

        return query;
    }

    private static IOrderedQueryable<Stay> ApplySorting(
        IQueryable<Stay> query,
        StayListRequest request)
    {
        var descending = request.SortDirection == SortDirection.Desc;

        return request.SortBy switch
        {
            StaySortBy.Price => descending
                ? query.OrderByDescending(x => x.Price).ThenByDescending(x => x.Rating).ThenBy(x => x.Name)
                : query.OrderBy(x => x.Price).ThenByDescending(x => x.Rating).ThenBy(x => x.Name),

            StaySortBy.Rating => descending
                ? query.OrderByDescending(x => x.Rating).ThenByDescending(x => x.Reviews).ThenBy(x => x.Price)
                : query.OrderBy(x => x.Rating).ThenByDescending(x => x.Reviews).ThenBy(x => x.Price),

            StaySortBy.Reviews => descending
                ? query.OrderByDescending(x => x.Reviews).ThenByDescending(x => x.Rating).ThenBy(x => x.Price)
                : query.OrderBy(x => x.Reviews).ThenByDescending(x => x.Rating).ThenBy(x => x.Price),

            StaySortBy.Name => descending
                ? query.OrderByDescending(x => x.Name)
                : query.OrderBy(x => x.Name),

            StaySortBy.Newest => descending
                ? query.OrderByDescending(x => x.CreatedAtUtc)
                : query.OrderBy(x => x.CreatedAtUtc),

            _ => query
                .OrderByDescending(x => x.Rating)
                .ThenByDescending(x => x.Reviews)
                .ThenBy(x => x.Price)
                .ThenBy(x => x.Name)
        };
    }

    private async Task<Stay?> FindExistingImportedStayAsync(
        string? cid,
        string name,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken)
    {
        var query = IncludeResponseData(_dbContext.Stays)
            .AsSplitQuery()
            .Where(x => x.SourceType == StaySourceType.ThirdParty);

        if (!string.IsNullOrWhiteSpace(cid))
        {
            return await query.FirstOrDefaultAsync(x => x.Cid == cid, cancellationToken);
        }

        return await query.FirstOrDefaultAsync(
            x => x.Name == name &&
                 x.Latitude == latitude &&
                 x.Longitude == longitude,
            cancellationToken);
    }

    private void ClearImportedCollections(Stay stay)
    {
        _dbContext.StayImages.RemoveRange(stay.Images);
        _dbContext.StayAmenities.RemoveRange(stay.Amenities);
        _dbContext.StayReviewsPerRatings.RemoveRange(stay.ReviewsPerRatings);
        _dbContext.StayBookingPlatforms.RemoveRange(stay.BookingPlatforms);
        _dbContext.StayReviews.RemoveRange(stay.StayReviews.Where(x => x.Platform != "glinter"));
    }

    private static StayResponse ToResponse(Stay stay)
    {
        return new StayResponse
        {
            Id = stay.Id,
            SourceType = stay.SourceType,
            CreatedByUserId = stay.CreatedByUserId,
            HotelOwnerProfileId = stay.HotelOwnerProfileId,
            Name = stay.Name,
            Price = stay.Price,
            Description = stay.Description,
            GoogleMapsLink = stay.GoogleMapsLink,
            Reviews = stay.Reviews,
            Rating = stay.Rating,
            Website = stay.Website,
            PhoneInternational = stay.PhoneInternational,
            LocationSummaryDescription = stay.LocationSummaryDescription,
            Cid = stay.Cid,
            Adm0Gid = stay.Adm0Gid,
            Adm1Gid = stay.Adm1Gid,
            Adm2Gid = stay.Adm2Gid,
            Adm3Gid = stay.Adm3Gid,
            Latitude = stay.Latitude,
            Longitude = stay.Longitude,
            Images = stay.Images
                .OrderBy(x => x.Id)
                .Select(x => new StayImageResponse { Id = x.Id, Link = x.Link })
                .ToList(),
            Amenities = stay.Amenities
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToList(),
            ReviewsPerRating = stay.ReviewsPerRatings
                .OrderBy(x => x.Rating)
                .Select(x => new StayReviewsPerRatingResponse
                {
                    Rating = x.Rating,
                    ReviewsCount = x.ReviewsCount
                })
                .ToList(),
            BookingPlatforms = stay.BookingPlatforms
                .OrderBy(x => x.PriceWithTax)
                .Select(x => new StayBookingPlatformResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    PriceWithTax = x.PriceWithTax,
                    Link = x.Link
                })
                .ToList(),
            FeaturedReviews = stay.StayReviews
                .OrderByDescending(x => x.PublishedAtDate ?? x.CreatedAtUtc)
                .Take(10)
                .Select(ToReviewResponse)
                .ToList()
        };
    }

    private static StayReviewResponse ToReviewResponse(StayReview review)
    {
        return new StayReviewResponse
        {
            Id = review.Id,
            ExternalReviewId = review.ExternalReviewId,
            ReviewerName = review.ReviewerName,
            Rating = review.Rating,
            ReviewText = review.ReviewText,
            Platform = review.Platform,
            PublishedAtDate = review.PublishedAtDate
        };
    }

    private static void AddImportedChildren(Stay stay, JsonElement item)
    {
        AddImages(stay, GetImageLinks(item));
        AddAmenities(stay, GetAmenityNames(item));
        AddReviewsPerRating(stay, item);
        AddBookingPlatforms(stay, GetBookingPlatformRequests(item));
        AddPartnerReviews(stay, item);
        AddGoogleReviews(stay, item);
    }

    private static void AddImages(Stay stay, IEnumerable<string?> links)
    {
        foreach (var link in links.Select(NormalizeString).Where(x => x is not null).Distinct())
        {
            stay.Images.Add(new StayImage { Link = link! });
        }
    }

    private static List<string?> GetImageLinks(JsonElement item)
    {
        var links = new List<string?>();
        var images = GetProperty(item, "images");

        if (images is not null && images.Value.ValueKind == JsonValueKind.Array)
        {
            links.AddRange(images.Value.EnumerateArray().Select(x => GetString(x, "link")));
        }

        var featuredImages = GetProperty(item, "featured_images");

        if (featuredImages is not null && featuredImages.Value.ValueKind == JsonValueKind.Array)
        {
            links.AddRange(featuredImages.Value.EnumerateArray().Select(x => GetString(x, "link")));
        }

        links.Add(GetString(item, "featured_image"));

        return links;
    }

    private static List<string> GetAmenityNames(JsonElement item)
    {
        var amenities = GetProperty(item, "amenities");
        var names = new List<string>();

        if (amenities is null || amenities.Value.ValueKind != JsonValueKind.Array)
        {
            return names;
        }

        foreach (var amenity in amenities.Value.EnumerateArray())
        {
            if (GetBool(amenity, "enabled") != true)
            {
                continue;
            }

            var name = CleanText(GetString(amenity, "name"));

            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name);
            }
        }

        return names;
    }

    private static void AddAmenities(Stay stay, IEnumerable<string?> names)
    {
        foreach (var name in names.Select(CleanText).Where(x => x is not null).Distinct())
        {
            stay.Amenities.Add(new StayAmenity { Name = name! });
        }
    }

    private static void AddReviewsPerRating(Stay stay, JsonElement item)
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

            stay.ReviewsPerRatings.Add(new StayReviewsPerRating
            {
                Rating = ratingValue,
                ReviewsCount = reviewsCount
            });
        }
    }

    private static List<StayBookingPlatformRequest> GetBookingPlatformRequests(JsonElement item)
    {
        var platforms = GetProperty(item, "booking_platforms");
        var result = new List<StayBookingPlatformRequest>();

        if (platforms is null || platforms.Value.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var platform in platforms.Value.EnumerateArray())
        {
            var name = CleanText(GetString(platform, "name"));

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            result.Add(new StayBookingPlatformRequest
            {
                Name = name,
                PriceWithTax = ParseMoney(GetString(platform, "price_with_tax")),
                Link = NormalizeString(GetString(platform, "link"))
            });
        }

        return result;
    }

    private static void AddBookingPlatforms(Stay stay, IEnumerable<StayBookingPlatformRequest> platforms)
    {
        foreach (var platform in platforms)
        {
            var name = CleanText(platform.Name);

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            stay.BookingPlatforms.Add(new StayBookingPlatform
            {
                Name = name,
                PriceWithTax = platform.PriceWithTax,
                Link = NormalizeString(platform.Link)
            });
        }
    }

    private static void AddPartnerReviews(Stay stay, JsonElement item)
    {
        var reviews = GetProperty(item, "featured_partner_reviews");

        if (reviews is null || reviews.Value.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var review in reviews.Value.EnumerateArray())
        {
            if (review.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var platformName = CleanText(GetNestedString(review, "platform", "name")) ?? "partner";
            var externalReviewId = BuildExternalReviewId(
                platformName,
                GetString(review, "link"),
                GetString(review, "reviewer"),
                GetString(review, "published_at_date"),
                GetString(review, "review_text"));

            stay.StayReviews.Add(new StayReview
            {
                ExternalReviewId = externalReviewId,
                ReviewerName = CleanText(GetString(review, "name")) ?? CleanText(GetString(review, "reviewer")),
                Rating = GetInt(review, "rating"),
                ReviewText = CleanText(GetString(review, "review_text")),
                Platform = platformName,
                PublishedAtDate = GetDateTime(review, "published_at_date")
            });
        }
    }

    private static void AddGoogleReviews(Stay stay, JsonElement item)
    {
        var reviews = GetProperty(item, "featured_reviews");

        if (reviews is null || reviews.Value.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var existingReviewIds = stay.StayReviews
            .Where(x => !string.IsNullOrWhiteSpace(x.ExternalReviewId))
            .Select(x => x.ExternalReviewId!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var review in reviews.Value.EnumerateArray())
        {
            if (review.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var reviewId = NormalizeExternalReviewId(
                "google maps",
                GetString(review, "review_id"),
                GetString(review, "review_link"),
                GetString(review, "name"),
                GetString(review, "published_at_date"),
                GetString(review, "review_text"));

            if (!string.IsNullOrWhiteSpace(reviewId) && !existingReviewIds.Add(reviewId))
            {
                continue;
            }

            stay.StayReviews.Add(new StayReview
            {
                ExternalReviewId = reviewId,
                ReviewerName = CleanText(GetString(review, "name")),
                Rating = GetInt(review, "rating"),
                ReviewText = CleanText(GetString(review, "review_text")),
                Platform = "google maps",
                PublishedAtDate = GetDateTime(review, "published_at_date")
            });
        }
    }

    private async Task AttachRegionHierarchyAsync(Stay stay, CancellationToken cancellationToken)
    {
        stay.Adm0Gid = null;
        stay.Adm1Gid = null;
        stay.Adm2Gid = null;
        stay.Adm3Gid = null;

        if (stay.Latitude is null || stay.Longitude is null)
        {
            return;
        }

        var hierarchy = await _regionsPointLookupRepository.GetHierarchyByPointAsync(
            stay.Latitude.Value,
            stay.Longitude.Value,
            cancellationToken);

        stay.Adm0Gid = hierarchy?.Adm0Gid;
        stay.Adm1Gid = hierarchy?.Adm1Gid;
        stay.Adm2Gid = hierarchy?.Adm2Gid;
        stay.Adm3Gid = hierarchy?.Adm3Gid;
    }

    private static string? NormalizeExternalReviewId(
        string platform,
        string? externalId,
        params string?[] fallbackParts)
    {
        var normalized = NormalizeString(externalId);

        return normalized is { Length: <= 200 }
            ? normalized
            : BuildExternalReviewId(platform, externalId, fallbackParts);
    }

    private static string BuildExternalReviewId(
        string platform,
        string? firstPart,
        params string?[] otherParts)
    {
        var raw = string.Join(
            "|",
            new[] { platform, firstPart }
                .Concat(otherParts)
                .Select(x => NormalizeString(x) ?? string.Empty));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return $"{platform.ToLowerInvariant().Replace(' ', '-')}:{Convert.ToHexString(bytes).ToLowerInvariant()}";
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

    private static string? GetNestedString(JsonElement item, string objectName, string propertyName)
    {
        var nested = GetProperty(item, objectName);
        return nested is null ? null : GetString(nested.Value, propertyName);
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

        return int.TryParse(NormalizeDigits(property.Value.ToString()), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
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

        return decimal.TryParse(NormalizeDigits(property.Value.ToString()), NumberStyles.Number, CultureInfo.InvariantCulture, out number)
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

    private static decimal? ParseMoney(string? value)
    {
        var normalized = NormalizeString(value);

        if (normalized is null)
        {
            return null;
        }

        normalized = NormalizeDigits(normalized);
        var builder = new StringBuilder();
        var started = false;

        foreach (var character in normalized)
        {
            if (char.IsDigit(character))
            {
                started = true;
                builder.Append(character);
                continue;
            }

            if (started && (character == ',' || character == '.'))
            {
                builder.Append(character);
                continue;
            }

            if (started)
            {
                break;
            }
        }

        var money = builder
            .ToString()
            .Replace(",", string.Empty, StringComparison.Ordinal);

        return decimal.TryParse(money, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
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
                '\u066B' => '.',
                '\u066C' => ',',
                _ => character
            });
        }

        return builder.ToString();
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
}
