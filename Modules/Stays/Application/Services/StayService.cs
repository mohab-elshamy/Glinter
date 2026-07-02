using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Communication.Application.Notifications.Commands;
using Glinter.Modules.Communication.Domain.Enums;
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
    private static readonly IReadOnlyDictionary<string, string> AmenityNameEnByNameAr =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["خدمة غسيل"] = "Laundry service",
            ["مكيّف هواء"] = "Air conditioning",
            ["خدمة غرف"] = "Room service",
            ["اتصال Wi-Fi مجاني"] = "Free Wi-Fi",
            ["مطعم"] = "Restaurant",
            ["حافلة للمطار"] = "Airport shuttle",
            ["مناسب للأطفال"] = "Kid-friendly",
            ["مُناسب لذوي الاحتياجات الخاصة"] = "Accessible",
            ["موقف سيارات مجاني"] = "Free parking",
            ["إفطار مجاني"] = "Free breakfast",
            ["Wi-Fi"] = "Wi-Fi",
            ["حمام سباحة خارجي"] = "Outdoor pool",
            ["صالة رياضة"] = "Fitness center",
            ["بار"] = "Bar",
            ["مطابخ في بعض الغرف"] = "Kitchen in some rooms",
            ["منتجع صحي"] = "Spa",
            ["إفطار مدفوع"] = "Paid breakfast",
            ["حوض استحمام ساخن"] = "Hot tub",
            ["موقف سيارات برسوم مدفوعة"] = "Paid parking",
            ["مركز أعمال"] = "Business center",
            ["الفطور"] = "Breakfast",
            ["يُحظر التدخين"] = "No smoking",
            ["مسبح"] = "Pool",
            ["مطبخ في جميع الغرف"] = "Kitchen in all rooms",
            ["موقف سيارات"] = "Parking",
            ["يُسمح بحيوانات أليفة"] = "Pet-friendly",
            ["حمام سباحة داخلي وخارجي"] = "Indoor and outdoor pool",
            ["ملعب غولف"] = "Golf course",
            ["اتصال Wi-Fi برسوم مدفوعة"] = "Paid Wi-Fi"
        };

    private readonly StaysDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProfilesReadService _profilesReadService;
    private readonly IRegionsPointLookupRepository _regionsPointLookupRepository;
    private readonly CreateNotificationHandler _createNotificationHandler;

    public StayService(
        StaysDbContext dbContext,
        ICurrentUserService currentUserService,
        IProfilesReadService profilesReadService,
        IRegionsPointLookupRepository regionsPointLookupRepository,
        CreateNotificationHandler createNotificationHandler)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _profilesReadService = profilesReadService;
        _regionsPointLookupRepository = regionsPointLookupRepository;
        _createNotificationHandler = createNotificationHandler;
    }

    public async Task<StayResponse> CreateOwnerStayAsync(
        CreateStayRequest request,
        CancellationToken cancellationToken)
    {
        ValidateStayRequest(request);

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
            Longitude = request.Longitude,
            Adm0Gid = request.Adm0Gid,
            Adm1Gid = request.Adm1Gid,
            Adm2Gid = request.Adm2Gid,
            Adm3Gid = request.Adm3Gid
        };

        await AttachRegionHierarchyAsync(stay, cancellationToken);
        AddImages(stay, request.ImageLinks);
        AddAmenities(stay, request.Amenities);
        AddBookingPlatforms(stay, request.BookingPlatforms);

        _dbContext.Stays.Add(stay);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(stay);
    }

    public async Task<List<StayResponse>> GetMyStaysAsync(CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();

        var stays = await IncludeResponseData(_dbContext.Stays.AsNoTracking())
            .Where(x => x.CreatedByUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return stays.Select(ToResponse).ToList();
    }

    public async Task<StayResponse?> UpdateOwnerStayAsync(
        int stayId,
        UpdateStayRequest request,
        CancellationToken cancellationToken)
    {
        ValidateStayRequest(request);

        var stay = await IncludeResponseData(_dbContext.Stays)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == stayId, cancellationToken);

        if (stay is null)
        {
            return null;
        }

        EnsureCanManage(stay);

        stay.Name = CleanText(request.Name) ?? string.Empty;
        stay.Price = request.Price;
        stay.Description = CleanText(request.Description);
        stay.GoogleMapsLink = NormalizeString(request.GoogleMapsLink);
        stay.Website = NormalizeString(request.Website);
        stay.PhoneInternational = NormalizeString(request.PhoneInternational);
        stay.LocationSummaryDescription = CleanText(request.LocationSummaryDescription);
        stay.Latitude = request.Latitude;
        stay.Longitude = request.Longitude;
        stay.Adm0Gid = request.Adm0Gid;
        stay.Adm1Gid = request.Adm1Gid;
        stay.Adm2Gid = request.Adm2Gid;
        stay.Adm3Gid = request.Adm3Gid;
        stay.UpdatedAtUtc = DateTime.UtcNow;

        _dbContext.StayImages.RemoveRange(stay.Images);
        _dbContext.StayAmenities.RemoveRange(stay.Amenities);
        _dbContext.StayBookingPlatforms.RemoveRange(stay.BookingPlatforms);
        stay.Images.Clear();
        stay.Amenities.Clear();
        stay.BookingPlatforms.Clear();

        await AttachRegionHierarchyAsync(stay, cancellationToken);
        AddImages(stay, request.ImageLinks);
        AddAmenities(stay, request.Amenities);
        AddBookingPlatforms(stay, request.BookingPlatforms);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(stay);
    }

    public async Task<StayResponse?> SetActiveAsync(
        int stayId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var stay = await IncludeResponseData(_dbContext.Stays)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == stayId, cancellationToken);

        if (stay is null)
        {
            return null;
        }

        EnsureCanManage(stay);
        stay.IsActive = isActive;
        stay.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(stay);
    }

    public async Task<StayBookingResponse?> CreateBookingAsync(
        int stayId,
        CreateStayBookingRequest request,
        CancellationToken cancellationToken)
    {
        ValidateBookingRequest(request);
        var userId = RequireCurrentUserId();
        var travelerProfileId = await _profilesReadService.GetTravelerProfileIdByUserIdAsync(
            userId,
            cancellationToken)
            ?? throw new InvalidOperationException("Create your traveler profile before booking a stay.");

        var stay = await _dbContext.Stays
            .FirstOrDefaultAsync(x => x.Id == stayId && x.IsActive, cancellationToken);

        if (stay is null)
        {
            return null;
        }

        if (stay.Price is null or <= 0)
        {
            throw new InvalidOperationException("This stay does not have a bookable nightly price.");
        }

        var hasOverlap = await _dbContext.StayBookings.AnyAsync(
            x => x.StayId == stayId &&
                 x.TravelerProfileId == travelerProfileId &&
                 x.Status != StayBookingStatus.Cancelled &&
                 request.CheckInDate < x.CheckOutDate &&
                 request.CheckOutDate > x.CheckInDate,
            cancellationToken);

        if (hasOverlap)
        {
            throw new InvalidOperationException("You already have an overlapping booking for this stay.");
        }

        var nights = request.CheckOutDate.DayNumber - request.CheckInDate.DayNumber;
        var booking = new StayBooking
        {
            StayId = stayId,
            Stay = stay,
            TravelerProfileId = travelerProfileId,
            CreatedByUserId = userId,
            GuestName = _currentUserService.Email ?? "Traveler",
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            GuestCount = request.GuestCount,
            TotalPrice = stay.Price.Value * nights
        };

        _dbContext.StayBookings.Add(booking);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (stay.CreatedByUserId is Guid ownerUserId && ownerUserId != userId)
        {
            await _createNotificationHandler.HandleAsync(
                new CreateNotificationCommand
                {
                    UserId = ownerUserId,
                    Type = NotificationType.Booking,
                    Title = "New stay booking",
                    Body = $"{booking.GuestName} requested {stay.Name}.",
                    LinkUrl = "/profile/me?tab=hotels",
                    SourceModule = "Stays",
                    SourceEntityType = "StayBooking",
                    SourceEntityId = booking.Id
                },
                cancellationToken);
        }
        return ToBookingResponse(booking);
    }

    public async Task<List<StayBookingResponse>> GetMyBookingsAsync(CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        var bookings = await _dbContext.StayBookings
            .AsNoTracking()
            .Include(x => x.Stay)
            .Where(x => x.CreatedByUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return bookings.Select(ToBookingResponse).ToList();
    }

    public async Task<List<StayBookingResponse>?> GetStayBookingsAsync(
        int stayId,
        CancellationToken cancellationToken)
    {
        var stay = await _dbContext.Stays
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == stayId, cancellationToken);

        if (stay is null)
        {
            return null;
        }

        EnsureCanManage(stay);

        var bookings = await _dbContext.StayBookings
            .AsNoTracking()
            .Include(x => x.Stay)
            .Where(x => x.StayId == stayId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return bookings.Select(ToBookingResponse).ToList();
    }

    public async Task<StayBookingResponse?> CancelBookingAsync(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        var booking = await _dbContext.StayBookings
            .Include(x => x.Stay)
            .FirstOrDefaultAsync(x => x.Id == bookingId, cancellationToken);

        if (booking is null)
        {
            return null;
        }

        var canManageStay = IsAdmin() || booking.Stay.CreatedByUserId == userId;
        if (booking.CreatedByUserId != userId && !canManageStay)
        {
            throw new UnauthorizedAccessException("You cannot cancel this booking.");
        }

        if (booking.Status == StayBookingStatus.Completed)
        {
            throw new InvalidOperationException("A completed booking cannot be cancelled.");
        }

        booking.Status = StayBookingStatus.Cancelled;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        var cancellationRecipient = booking.CreatedByUserId == userId
            ? booking.Stay.CreatedByUserId
            : booking.CreatedByUserId;
        if (cancellationRecipient is Guid recipientUserId && recipientUserId != userId)
        {
            await _createNotificationHandler.HandleAsync(
                new CreateNotificationCommand
                {
                    UserId = recipientUserId,
                    Type = NotificationType.Booking,
                    Title = "Stay booking cancelled",
                    Body = $"The booking for {booking.Stay.Name} was cancelled.",
                    LinkUrl = booking.CreatedByUserId == userId
                        ? "/profile/me?tab=hotels"
                        : "/profile/me?tab=bookings",
                    SourceModule = "Stays",
                    SourceEntityType = "StayBooking",
                    SourceEntityId = booking.Id
                },
                cancellationToken);
        }
        return ToBookingResponse(booking);
    }

    public async Task<StayBookingResponse?> UpdateBookingStatusAsync(
        Guid bookingId,
        StayBookingStatus status,
        CancellationToken cancellationToken)
    {
        if (status is not (StayBookingStatus.Confirmed or StayBookingStatus.Completed or StayBookingStatus.Cancelled))
        {
            throw new ArgumentException("Booking status must be Confirmed, Completed, or Cancelled.");
        }

        var booking = await _dbContext.StayBookings
            .Include(x => x.Stay)
            .FirstOrDefaultAsync(x => x.Id == bookingId, cancellationToken);

        if (booking is null)
        {
            return null;
        }

        EnsureCanManage(booking.Stay);

        if (booking.Status == StayBookingStatus.Cancelled)
        {
            throw new InvalidOperationException("A cancelled booking cannot be changed.");
        }

        booking.Status = status;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _createNotificationHandler.HandleAsync(
            new CreateNotificationCommand
            {
                UserId = booking.CreatedByUserId,
                Type = NotificationType.Booking,
                Title = $"Stay booking {status.ToString().ToLowerInvariant()}",
                Body = $"Your booking for {booking.Stay.Name} is now {status.ToString().ToLowerInvariant()}.",
                LinkUrl = "/profile/me?tab=bookings",
                SourceModule = "Stays",
                SourceEntityType = "StayBooking",
                SourceEntityId = booking.Id
            },
            cancellationToken);
        return ToBookingResponse(booking);
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
        var query = ApplyFilters(_dbContext.Stays.AsNoTracking().Where(x => x.IsActive), request);

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
        var stay = await IncludeResponseData(_dbContext.Stays.AsNoTracking().Where(x => x.IsActive))
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return stay is null ? null : ToResponse(stay);
    }

    public async Task<List<StayRegionStatsResponse>> GetRegionStatsAsync(
        StayRegionStatsRequest request,
        CancellationToken cancellationToken)
    {
        var query = ApplyStatsFilters(_dbContext.Stays.AsNoTracking().Where(x => x.IsActive), request);
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
                ? query.OrderBy(x => x.Price == null).ThenByDescending(x => x.Price).ThenByDescending(x => x.Rating).ThenBy(x => x.Name)
                : query.OrderBy(x => x.Price == null).ThenBy(x => x.Price).ThenByDescending(x => x.Rating).ThenBy(x => x.Name),

            StaySortBy.Rating => descending
                ? query.OrderBy(x => x.Rating == null).ThenByDescending(x => x.Rating).ThenByDescending(x => x.Reviews).ThenBy(x => x.Price)
                : query.OrderBy(x => x.Rating == null).ThenBy(x => x.Rating).ThenByDescending(x => x.Reviews).ThenBy(x => x.Price),

            StaySortBy.Reviews => descending
                ? query.OrderBy(x => x.Reviews == null).ThenByDescending(x => x.Reviews).ThenByDescending(x => x.Rating).ThenBy(x => x.Price)
                : query.OrderBy(x => x.Reviews == null).ThenBy(x => x.Reviews).ThenByDescending(x => x.Rating).ThenBy(x => x.Price),

            StaySortBy.Name => descending
                ? query.OrderByDescending(x => x.Name)
                : query.OrderBy(x => x.Name),

            StaySortBy.Newest => descending
                ? query.OrderByDescending(x => x.CreatedAtUtc)
                : query.OrderBy(x => x.CreatedAtUtc),

            _ => query
                .OrderBy(x => x.Rating == null)
                .ThenByDescending(x => x.Rating)
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
            IsActive = stay.IsActive,
            CreatedAtUtc = stay.CreatedAtUtc,
            UpdatedAtUtc = stay.UpdatedAtUtc,
            Images = stay.Images
                .OrderBy(x => x.Id)
                .Select(x => new StayImageResponse { Id = x.Id, Link = x.Link })
                .ToList(),
            Amenities = stay.Amenities
                .Select(GetAmenityDisplayName)
                .Where(x => x is not null)
                .Select(x => x!)
                .OrderBy(x => x)
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

    private static StayBookingResponse ToBookingResponse(StayBooking booking)
    {
        return new StayBookingResponse
        {
            Id = booking.Id,
            StayId = booking.StayId,
            StayName = booking.Stay.Name,
            TravelerProfileId = booking.TravelerProfileId,
            GuestName = booking.GuestName,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            GuestCount = booking.GuestCount,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status,
            CreatedAtUtc = booking.CreatedAtUtc,
            UpdatedAtUtc = booking.UpdatedAtUtc
        };
    }

    private static void ValidateStayRequest(CreateStayRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Stay name is required.");
        }

        if (request.Price is null or <= 0)
        {
            throw new ArgumentException("A positive nightly price is required.");
        }

        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
        {
            throw new ArgumentException("Valid latitude and longitude are required.");
        }

        if (new[] { request.Adm0Gid, request.Adm1Gid, request.Adm2Gid, request.Adm3Gid }
            .Any(x => x is <= 0))
        {
            throw new ArgumentException("Region identifiers must be positive.");
        }
    }

    private static void ValidateBookingRequest(CreateStayBookingRequest request)
    {
        if (request.CheckInDate == default || request.CheckOutDate == default)
        {
            throw new ArgumentException("Check-in and check-out dates are required.");
        }

        if (request.CheckOutDate <= request.CheckInDate)
        {
            throw new ArgumentException("Check-out must be after check-in.");
        }

        if (request.CheckInDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentException("Check-in cannot be in the past.");
        }

        if (request.GuestCount is < 1 or > 100)
        {
            throw new ArgumentException("Guest count must be between 1 and 100.");
        }
    }

    private Guid RequireCurrentUserId()
    {
        return _currentUserService.UserId
            ?? throw new InvalidOperationException("Authenticated user id is missing.");
    }

    private bool IsAdmin() => _currentUserService.Roles.Contains(RoleNames.Admin);

    private void EnsureCanManage(Stay stay)
    {
        if (!IsAdmin() && stay.CreatedByUserId != RequireCurrentUserId())
        {
            throw new UnauthorizedAccessException("You cannot manage this stay.");
        }
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
            stay.Amenities.Add(CreateAmenity(name!));
        }
    }

    private static StayAmenity CreateAmenity(string name)
    {
        return ContainsArabic(name)
            ? new StayAmenity
            {
                NameAr = name,
                NameEn = AmenityNameEnByNameAr.GetValueOrDefault(name)
            }
            : new StayAmenity { NameEn = name };
    }

    private static string? GetAmenityDisplayName(StayAmenity amenity)
    {
        return !string.IsNullOrWhiteSpace(amenity.NameEn)
            ? amenity.NameEn
            : amenity.NameAr;
    }

    private static bool ContainsArabic(string value)
    {
        return value.Any(c => c is >= '\u0600' and <= '\u06FF');
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
        if (stay.Adm0Gid is not null ||
            stay.Adm1Gid is not null ||
            stay.Adm2Gid is not null ||
            stay.Adm3Gid is not null)
        {
            return;
        }

        if (stay.Latitude is null || stay.Longitude is null)
        {
            return;
        }

        var hierarchy = await _regionsPointLookupRepository.GetHierarchyByPointAsync(
            stay.Latitude.Value,
            stay.Longitude.Value,
            cancellationToken);

        if (hierarchy is not null)
        {
            stay.Adm0Gid = hierarchy.Adm0Gid;
            stay.Adm1Gid = hierarchy.Adm1Gid;
            stay.Adm2Gid = hierarchy.Adm2Gid;
            stay.Adm3Gid = hierarchy.Adm3Gid;
        }
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
