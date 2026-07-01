using System.Globalization;
using System.Text;
using System.Text.Json;
using Glinter.Modules.Experiences.Application.Dtos;
using Glinter.Modules.Experiences.Domain.Entities;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.Communication.Application.Notifications.Commands;
using Glinter.Modules.Communication.Domain.Enums;
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
    private readonly CreateNotificationHandler _createNotificationHandler;

    public ExperienceService(
        ExperiencesDbContext dbContext,
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

    public async Task<ExperienceResponse> CreateProviderExperienceAsync(
        CreateExperienceRequest request,
        CancellationToken cancellationToken)
    {
        ValidateExperienceRequest(request);

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
            ModerationStatus = ExperienceModerationStatus.Pending,
            CreatedByUserId = userId,
            ProviderProfileId = providerProfileId,
            Name = CleanText(request.Name) ?? string.Empty,
            Description = CleanText(request.Description),
            Address = CleanText(request.Address),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Adm0Gid = request.Adm0Gid,
            Adm1Gid = request.Adm1Gid,
            Adm2Gid = request.Adm2Gid,
            Adm3Gid = request.Adm3Gid,
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

    public async Task<List<ExperienceResponse>> GetMyExperiencesAsync(CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        var experiences = await IncludeResponseData(_dbContext.Experiences.AsNoTracking())
            .Where(x => x.CreatedByUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var now = DateTime.Now;
        return experiences.Select(x => ToResponse(x, now)).ToList();
    }

    public async Task<List<ExperienceResponse>> GetAdminExperiencesAsync(
        ExperienceModerationStatus? moderationStatus,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = IncludeResponseData(_dbContext.Experiences.AsNoTracking());
        if (moderationStatus is not null)
        {
            query = query.Where(x => x.ModerationStatus == moderationStatus);
        }
        if (isActive is not null)
        {
            query = query.Where(x => x.IsActive == isActive);
        }

        var experiences = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        var now = DateTime.Now;
        return experiences.Select(x => ToResponse(x, now)).ToList();
    }

    public async Task<ExperienceResponse?> ModerateAsync(
        int experienceId,
        ModerateExperienceRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.ModerationStatus))
        {
            throw new ArgumentException("A valid moderation status is required.");
        }
        if (request.ModerationStatus == ExperienceModerationStatus.Rejected &&
            string.IsNullOrWhiteSpace(request.ModerationNotes))
        {
            throw new ArgumentException("Moderation notes are required when rejecting an experience.");
        }

        var experience = await IncludeResponseData(_dbContext.Experiences)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == experienceId, cancellationToken);
        if (experience is null)
        {
            return null;
        }

        experience.ModerationStatus = request.ModerationStatus;
        experience.ModerationNotes = CleanText(request.ModerationNotes);
        experience.ModeratedByUserId = RequireCurrentUserId();
        experience.ModeratedAtUtc = DateTime.UtcNow;
        experience.UpdatedAtUtc = DateTime.UtcNow;
        experience.IsActive = request.ModerationStatus != ExperienceModerationStatus.Rejected;

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (experience.CreatedByUserId is Guid providerUserId)
        {
            var statusText = request.ModerationStatus.ToString().ToLowerInvariant();
            await _createNotificationHandler.HandleAsync(
                new CreateNotificationCommand
                {
                    UserId = providerUserId,
                    Type = NotificationType.Moderation,
                    Title = $"Experience {statusText}",
                    Body = $"{experience.Name} was {statusText}. Open your provider dashboard for details.",
                    LinkUrl = "/profile/me?tab=experiences",
                    SourceModule = "Experiences",
                    SourceEntityType = "Experience"
                },
                cancellationToken);
        }
        return ToResponse(experience, DateTime.Now);
    }

    public async Task<ExperienceResponse?> UpdateProviderExperienceAsync(
        int experienceId,
        UpdateExperienceRequest request,
        CancellationToken cancellationToken)
    {
        ValidateExperienceRequest(request);
        var experience = await IncludeResponseData(_dbContext.Experiences)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == experienceId, cancellationToken);

        if (experience is null)
        {
            return null;
        }

        EnsureCanManage(experience);
        experience.Category = request.Category;
        experience.Name = CleanText(request.Name) ?? string.Empty;
        experience.Description = CleanText(request.Description);
        experience.Address = CleanText(request.Address);
        experience.Latitude = request.Latitude;
        experience.Longitude = request.Longitude;
        experience.Adm0Gid = request.Adm0Gid;
        experience.Adm1Gid = request.Adm1Gid;
        experience.Adm2Gid = request.Adm2Gid;
        experience.Adm3Gid = request.Adm3Gid;
        experience.GoogleMapsLink = NormalizeString(request.GoogleMapsLink);
        experience.PhoneInternational = NormalizeString(request.PhoneInternational);
        experience.PriceRange = NormalizeString(request.PriceRange);
        experience.Website = NormalizeString(request.Website);
        experience.UpdatedAtUtc = DateTime.UtcNow;

        _dbContext.ExperienceFeaturedImages.RemoveRange(experience.FeaturedImages);
        _dbContext.ExperienceHours.RemoveRange(experience.Hours);
        _dbContext.ExperiencePopularTimes.RemoveRange(experience.PopularTimes);
        _dbContext.ExperienceAmenities.RemoveRange(experience.Amenities);
        experience.FeaturedImages.Clear();
        experience.Hours.Clear();
        experience.PopularTimes.Clear();
        experience.Amenities.Clear();

        await AttachRegionHierarchyAsync(experience, cancellationToken);
        AddProviderChildren(experience, request);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(experience, DateTime.Now);
    }

    public async Task<ExperienceResponse?> SetActiveAsync(
        int experienceId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var experience = await IncludeResponseData(_dbContext.Experiences)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == experienceId, cancellationToken);

        if (experience is null)
        {
            return null;
        }

        EnsureCanManage(experience);
        experience.IsActive = isActive;
        experience.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(experience, DateTime.Now);
    }

    public async Task<List<ExperienceAvailabilityResponse>?> GetAvailabilityAsync(
        int experienceId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var experience = await _dbContext.Experiences
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == experienceId, cancellationToken);

        if (experience is null ||
            (!includeInactive &&
             (!experience.IsActive ||
              experience.ModerationStatus != ExperienceModerationStatus.Approved)))
        {
            return null;
        }

        if (includeInactive)
        {
            EnsureCanManage(experience);
        }

        var query = _dbContext.ExperienceAvailabilitySlots
            .AsNoTracking()
            .Include(x => x.Bookings)
            .Where(x => x.ExperienceId == experienceId);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive && x.StartTimeUtc > DateTime.UtcNow);
        }

        var slots = await query.OrderBy(x => x.StartTimeUtc).ToListAsync(cancellationToken);
        return slots.Select(ToAvailabilityResponse).ToList();
    }

    public async Task<ExperienceAvailabilityResponse?> CreateAvailabilityAsync(
        int experienceId,
        CreateExperienceAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        ValidateAvailabilityRequest(request);
        var experience = await _dbContext.Experiences
            .FirstOrDefaultAsync(x => x.Id == experienceId, cancellationToken);

        if (experience is null)
        {
            return null;
        }

        EnsureCanManage(experience);
        var slot = new ExperienceAvailability
        {
            ExperienceId = experienceId,
            StartTimeUtc = request.StartTimeUtc.ToUniversalTime(),
            EndTimeUtc = request.EndTimeUtc.ToUniversalTime(),
            Capacity = request.Capacity,
            PricePerPerson = request.PricePerPerson
        };
        _dbContext.ExperienceAvailabilitySlots.Add(slot);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToAvailabilityResponse(slot);
    }

    public async Task<ExperienceAvailabilityResponse?> UpdateAvailabilityAsync(
        Guid availabilityId,
        UpdateExperienceAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        ValidateAvailabilityRequest(request);
        var slot = await _dbContext.ExperienceAvailabilitySlots
            .Include(x => x.Experience)
            .Include(x => x.Bookings)
            .FirstOrDefaultAsync(x => x.Id == availabilityId, cancellationToken);

        if (slot is null)
        {
            return null;
        }

        EnsureCanManage(slot.Experience);
        var bookedGuests = ActiveGuests(slot.Bookings);
        if (request.Capacity < bookedGuests)
        {
            throw new InvalidOperationException("Capacity cannot be lower than the number of booked guests.");
        }

        slot.StartTimeUtc = request.StartTimeUtc.ToUniversalTime();
        slot.EndTimeUtc = request.EndTimeUtc.ToUniversalTime();
        slot.Capacity = request.Capacity;
        slot.PricePerPerson = request.PricePerPerson;
        slot.IsActive = request.IsActive;
        slot.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToAvailabilityResponse(slot);
    }

    public async Task<ExperienceAvailabilityResponse?> SetAvailabilityActiveAsync(
        Guid availabilityId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var slot = await _dbContext.ExperienceAvailabilitySlots
            .Include(x => x.Experience)
            .Include(x => x.Bookings)
            .FirstOrDefaultAsync(x => x.Id == availabilityId, cancellationToken);

        if (slot is null)
        {
            return null;
        }

        EnsureCanManage(slot.Experience);
        slot.IsActive = isActive;
        slot.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToAvailabilityResponse(slot);
    }

    public async Task<ExperienceBookingResponse?> CreateBookingAsync(
        int experienceId,
        CreateExperienceBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (request.GuestsCount is < 1 or > 100)
        {
            throw new ArgumentException("Guest count must be between 1 and 100.");
        }

        var userId = RequireCurrentUserId();
        var travelerProfileId = await _profilesReadService.GetTravelerProfileIdByUserIdAsync(
            userId,
            cancellationToken)
            ?? throw new InvalidOperationException("Create your traveler profile before booking an experience.");

        var slot = await _dbContext.ExperienceAvailabilitySlots
            .Include(x => x.Experience)
            .Include(x => x.Bookings)
            .FirstOrDefaultAsync(
                x => x.Id == request.AvailabilityId && x.ExperienceId == experienceId,
                cancellationToken);

        if (slot is null ||
            !slot.IsActive ||
            !slot.Experience.IsActive ||
            slot.Experience.ModerationStatus != ExperienceModerationStatus.Approved)
        {
            return null;
        }

        if (slot.StartTimeUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("This availability slot has already started.");
        }

        if (slot.Capacity - ActiveGuests(slot.Bookings) < request.GuestsCount)
        {
            throw new InvalidOperationException("This availability slot does not have enough remaining capacity.");
        }

        if (slot.Bookings.Any(x =>
                x.CreatedByUserId == userId &&
                x.Status != ExperienceBookingStatus.Cancelled))
        {
            throw new InvalidOperationException("You already booked this availability slot.");
        }

        var booking = new ExperienceBooking
        {
            ExperienceId = experienceId,
            Experience = slot.Experience,
            AvailabilityId = slot.Id,
            Availability = slot,
            TravelerProfileId = travelerProfileId,
            CreatedByUserId = userId,
            TravelerName = _currentUserService.Email ?? "Traveler",
            GuestsCount = request.GuestsCount,
            TotalPrice = slot.PricePerPerson * request.GuestsCount
        };
        _dbContext.ExperienceBookings.Add(booking);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (slot.Experience.CreatedByUserId is Guid providerUserId &&
            providerUserId != userId)
        {
            await _createNotificationHandler.HandleAsync(
                new CreateNotificationCommand
                {
                    UserId = providerUserId,
                    Type = NotificationType.Booking,
                    Title = "New experience booking",
                    Body = $"{booking.TravelerName} requested {slot.Experience.Name}.",
                    LinkUrl = "/profile/me?tab=experiences",
                    SourceModule = "Experiences",
                    SourceEntityType = "ExperienceBooking",
                    SourceEntityId = booking.Id
                },
                cancellationToken);
        }
        return ToBookingResponse(booking);
    }

    public async Task<List<ExperienceBookingResponse>> GetMyBookingsAsync(CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        var bookings = await BookingQuery()
            .AsNoTracking()
            .Where(x => x.CreatedByUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return bookings.Select(ToBookingResponse).ToList();
    }

    public async Task<List<ExperienceBookingResponse>?> GetExperienceBookingsAsync(
        int experienceId,
        CancellationToken cancellationToken)
    {
        var experience = await _dbContext.Experiences
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == experienceId, cancellationToken);
        if (experience is null)
        {
            return null;
        }

        EnsureCanManage(experience);
        var bookings = await BookingQuery()
            .AsNoTracking()
            .Where(x => x.ExperienceId == experienceId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return bookings.Select(ToBookingResponse).ToList();
    }

    public async Task<ExperienceBookingResponse?> CancelBookingAsync(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        var booking = await BookingQuery()
            .FirstOrDefaultAsync(x => x.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return null;
        }

        if (booking.CreatedByUserId != userId &&
            !IsAdmin() &&
            booking.Experience.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("You cannot cancel this booking.");
        }

        if (booking.Status == ExperienceBookingStatus.Completed)
        {
            throw new InvalidOperationException("A completed booking cannot be cancelled.");
        }

        booking.Status = ExperienceBookingStatus.Cancelled;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        var cancellationRecipient = booking.CreatedByUserId == userId
            ? booking.Experience.CreatedByUserId
            : booking.CreatedByUserId;
        if (cancellationRecipient is Guid recipientUserId && recipientUserId != userId)
        {
            await _createNotificationHandler.HandleAsync(
                new CreateNotificationCommand
                {
                    UserId = recipientUserId,
                    Type = NotificationType.Booking,
                    Title = "Experience booking cancelled",
                    Body = $"The booking for {booking.Experience.Name} was cancelled.",
                    LinkUrl = booking.CreatedByUserId == userId
                        ? "/profile/me?tab=experiences"
                        : "/profile/me?tab=bookings",
                    SourceModule = "Experiences",
                    SourceEntityType = "ExperienceBooking",
                    SourceEntityId = booking.Id
                },
                cancellationToken);
        }
        return ToBookingResponse(booking);
    }

    public async Task<ExperienceBookingResponse?> UpdateBookingStatusAsync(
        Guid bookingId,
        ExperienceBookingStatus status,
        CancellationToken cancellationToken)
    {
        if (status is not (ExperienceBookingStatus.Confirmed or ExperienceBookingStatus.Completed or ExperienceBookingStatus.Cancelled))
        {
            throw new ArgumentException("Booking status must be Confirmed, Completed, or Cancelled.");
        }

        var booking = await BookingQuery()
            .FirstOrDefaultAsync(x => x.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return null;
        }

        EnsureCanManage(booking.Experience);
        if (booking.Status == ExperienceBookingStatus.Cancelled)
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
                Title = $"Experience booking {status.ToString().ToLowerInvariant()}",
                Body = $"Your booking for {booking.Experience.Name} is now {status.ToString().ToLowerInvariant()}.",
                LinkUrl = "/profile/me?tab=bookings",
                SourceModule = "Experiences",
                SourceEntityType = "ExperienceBooking",
                SourceEntityId = booking.Id
            },
            cancellationToken);
        return ToBookingResponse(booking);
    }

    public async Task<ExperienceReviewResponse?> CreateReviewAsync(
        int experienceId,
        CreateExperienceReviewRequest request,
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

        var experience = await _dbContext.Experiences
            .FirstOrDefaultAsync(
                x => x.Id == experienceId &&
                     x.IsActive &&
                     x.ModerationStatus == ExperienceModerationStatus.Approved,
                cancellationToken);
        if (experience is null)
        {
            return null;
        }

        var review = new ExperienceReview
        {
            ExperienceId = experienceId,
            CreatedByUserId = RequireCurrentUserId(),
            ReviewerName = _currentUserService.Email,
            Rating = request.Rating,
            ReviewText = CleanText(request.ReviewText),
            SourceList = "glinter",
            PublishedAtDate = DateTime.UtcNow
        };
        _dbContext.ExperienceReviews.Add(review);
        var previousReviews = Math.Max(0, experience.Reviews ?? 0);
        var previousRating = experience.Rating ?? 0;
        experience.Reviews = previousReviews + 1;
        experience.Rating = Math.Round(
            ((previousRating * previousReviews) + request.Rating) / experience.Reviews.Value,
            2);
        experience.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToReviewResponse(review);
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
                ModerationStatus = ExperienceModerationStatus.Approved,
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
        ValidateListRequest(request);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var now = DateTime.Now;
        var query = ApplyFilters(
            _dbContext.Experiences.AsNoTracking().Where(
                x => x.IsActive &&
                     x.ModerationStatus == ExperienceModerationStatus.Approved),
            request);

        var totalCount = await query.CountAsync(cancellationToken);
        var experiences = await IncludeResponseData(ApplySorting(query, request, now))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

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
        ValidateListRequest(request);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var now = DateTime.Now;
        var query = ApplyFilters(
                _dbContext.Experiences.AsNoTracking().Where(
                    x => x.IsActive &&
                         x.ModerationStatus == ExperienceModerationStatus.Approved),
                request)
            .Where(x => x.Latitude != null && x.Longitude != null);
        var experiences = await ApplySorting(query, request, now)
            .Include(x => x.FeaturedImages)
            .Include(x => x.Hours)
            .Include(x => x.PopularTimes)
            .Include(x => x.AvailabilitySlots)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

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
                    StartingPricePerPerson = GetStartingPricePerPerson(x, now),
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
        var experience = await IncludeResponseData(
                _dbContext.Experiences.AsNoTracking().Where(
                    x => x.IsActive &&
                         x.ModerationStatus == ExperienceModerationStatus.Approved))
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

    private IQueryable<ExperienceBooking> BookingQuery()
    {
        return _dbContext.ExperienceBookings
            .Include(x => x.Experience)
            .Include(x => x.Availability);
    }

    private static ExperienceAvailabilityResponse ToAvailabilityResponse(
        ExperienceAvailability availability)
    {
        return new ExperienceAvailabilityResponse
        {
            Id = availability.Id,
            ExperienceId = availability.ExperienceId,
            StartTimeUtc = availability.StartTimeUtc,
            EndTimeUtc = availability.EndTimeUtc,
            Capacity = availability.Capacity,
            RemainingCapacity = Math.Max(0, availability.Capacity - ActiveGuests(availability.Bookings)),
            PricePerPerson = availability.PricePerPerson,
            IsActive = availability.IsActive
        };
    }

    private static ExperienceBookingResponse ToBookingResponse(ExperienceBooking booking)
    {
        return new ExperienceBookingResponse
        {
            Id = booking.Id,
            ExperienceId = booking.ExperienceId,
            ExperienceName = booking.Experience.Name,
            AvailabilityId = booking.AvailabilityId,
            TravelerProfileId = booking.TravelerProfileId,
            TravelerName = booking.TravelerName,
            StartTimeUtc = booking.Availability.StartTimeUtc,
            EndTimeUtc = booking.Availability.EndTimeUtc,
            GuestsCount = booking.GuestsCount,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status,
            CreatedAtUtc = booking.CreatedAtUtc,
            UpdatedAtUtc = booking.UpdatedAtUtc
        };
    }

    private static int ActiveGuests(IEnumerable<ExperienceBooking> bookings)
    {
        return bookings
            .Where(x => x.Status != ExperienceBookingStatus.Cancelled)
            .Sum(x => x.GuestsCount);
    }

    private static void ValidateExperienceRequest(CreateExperienceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Experience name is required.");
        }

        if (!Enum.IsDefined(request.Category))
        {
            throw new ArgumentException("A valid experience category is required.");
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

        if (request.Hours.Any(x => x.ClosesAt <= x.OpensAt))
        {
            throw new ArgumentException("Closing time must be after opening time.");
        }
    }

    private static void ValidateAvailabilityRequest(CreateExperienceAvailabilityRequest request)
    {
        if (request.StartTimeUtc == default || request.EndTimeUtc == default)
        {
            throw new ArgumentException("Start and end times are required.");
        }
        if (request.EndTimeUtc <= request.StartTimeUtc)
        {
            throw new ArgumentException("End time must be after start time.");
        }
        if (request.StartTimeUtc.ToUniversalTime() <= DateTime.UtcNow)
        {
            throw new ArgumentException("Availability must start in the future.");
        }
        if (request.Capacity is < 1 or > 10000)
        {
            throw new ArgumentException("Capacity must be between 1 and 10000.");
        }
        if (request.PricePerPerson < 0)
        {
            throw new ArgumentException("Price per person cannot be negative.");
        }
    }

    private Guid RequireCurrentUserId()
    {
        return _currentUserService.UserId
            ?? throw new InvalidOperationException("Authenticated user id is missing.");
    }

    private bool IsAdmin() => _currentUserService.Roles.Contains(RoleNames.Admin);

    private void EnsureCanManage(Experience experience)
    {
        if (!IsAdmin() && experience.CreatedByUserId != RequireCurrentUserId())
        {
            throw new UnauthorizedAccessException("You cannot manage this experience.");
        }
    }

    private static IQueryable<Experience> IncludeResponseData(IQueryable<Experience> query)
    {
        return query
            .Include(x => x.FeaturedImages)
            .Include(x => x.Hours)
            .Include(x => x.PopularTimes)
            .Include(x => x.ReviewsPerRatings)
            .Include(x => x.Amenities)
            .Include(x => x.ExperienceReviews)
            .Include(x => x.AvailabilitySlots);
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

        if (request.IsFree is true)
        {
            var nowUtc = DateTime.UtcNow;
            query = query.Where(x =>
                x.AvailabilitySlots.Any(slot =>
                    slot.IsActive &&
                    slot.StartTimeUtc > nowUtc &&
                    slot.PricePerPerson == 0) ||
                (!x.AvailabilitySlots.Any(slot =>
                     slot.IsActive && slot.StartTimeUtc > nowUtc) &&
                 x.PriceRange != null &&
                 x.PriceRange.ToLower() == "free"));
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

    private static IOrderedQueryable<Experience> ApplySorting(
        IQueryable<Experience> query,
        ExperienceListRequest request,
        DateTime now)
    {
        var descending = request.SortDirection == ExperienceSortDirection.Desc;
        var nowUtc = now.Kind == DateTimeKind.Utc ? now : now.ToUniversalTime();
        var day = now.DayOfWeek;
        var time = TimeOnly.FromDateTime(now);

        return request.SortBy switch
        {
            ExperienceSortBy.Price => descending
                ? query
                    .OrderBy(x => !x.AvailabilitySlots.Any(slot =>
                        slot.IsActive && slot.StartTimeUtc > nowUtc))
                    .ThenByDescending(x => x.AvailabilitySlots
                        .Where(slot => slot.IsActive && slot.StartTimeUtc > nowUtc)
                        .Min(slot => (decimal?)slot.PricePerPerson))
                    .ThenBy(x => x.Name)
                    .ThenBy(x => x.Id)
                : query
                    .OrderBy(x => !x.AvailabilitySlots.Any(slot =>
                        slot.IsActive && slot.StartTimeUtc > nowUtc))
                    .ThenBy(x => x.AvailabilitySlots
                        .Where(slot => slot.IsActive && slot.StartTimeUtc > nowUtc)
                        .Min(slot => (decimal?)slot.PricePerPerson))
                    .ThenBy(x => x.Name)
                    .ThenBy(x => x.Id),

            ExperienceSortBy.Rating => descending
                ? query.OrderBy(x => x.Rating == null).ThenByDescending(x => x.Rating).ThenByDescending(x => x.Reviews).ThenBy(x => x.Name).ThenBy(x => x.Id)
                : query.OrderBy(x => x.Rating == null).ThenBy(x => x.Rating).ThenByDescending(x => x.Reviews).ThenBy(x => x.Name).ThenBy(x => x.Id),

            ExperienceSortBy.Reviews => descending
                ? query.OrderBy(x => x.Reviews == null).ThenByDescending(x => x.Reviews).ThenByDescending(x => x.Rating).ThenBy(x => x.Name).ThenBy(x => x.Id)
                : query.OrderBy(x => x.Reviews == null).ThenBy(x => x.Reviews).ThenByDescending(x => x.Rating).ThenBy(x => x.Name).ThenBy(x => x.Id),

            ExperienceSortBy.Name => descending
                ? query.OrderByDescending(x => x.Name).ThenBy(x => x.Id)
                : query.OrderBy(x => x.Name).ThenBy(x => x.Id),

            ExperienceSortBy.Newest => descending
                ? query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
                : query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id),

            ExperienceSortBy.Popularity => descending
                ? query
                    .OrderBy(x => !x.PopularTimes.Any(item =>
                        item.DayOfWeek == day && item.HourOfDay == now.Hour))
                    .ThenByDescending(x => x.PopularTimes
                        .Where(item => item.DayOfWeek == day && item.HourOfDay == now.Hour)
                        .Select(item => (int?)item.PopularityPercentage)
                        .FirstOrDefault())
                    .ThenBy(x => x.Rating == null)
                    .ThenByDescending(x => x.Rating)
                    .ThenBy(x => x.Name)
                    .ThenBy(x => x.Id)
                : query
                    .OrderBy(x => !x.PopularTimes.Any(item =>
                        item.DayOfWeek == day && item.HourOfDay == now.Hour))
                    .ThenBy(x => x.PopularTimes
                        .Where(item => item.DayOfWeek == day && item.HourOfDay == now.Hour)
                        .Select(item => (int?)item.PopularityPercentage)
                        .FirstOrDefault())
                    .ThenBy(x => x.Rating == null)
                    .ThenByDescending(x => x.Rating)
                    .ThenBy(x => x.Name)
                    .ThenBy(x => x.Id),

            ExperienceSortBy.OpenNow => descending
                ? query
                    .OrderByDescending(x => x.Hours.Any(hour =>
                        hour.DayOfWeek == day && hour.OpensAt <= time && hour.ClosesAt > time))
                    .ThenBy(x => x.Rating == null)
                    .ThenByDescending(x => x.Rating)
                    .ThenBy(x => x.Name)
                    .ThenBy(x => x.Id)
                : query
                    .OrderBy(x => x.Hours.Any(hour =>
                        hour.DayOfWeek == day && hour.OpensAt <= time && hour.ClosesAt > time))
                    .ThenBy(x => x.Rating == null)
                    .ThenByDescending(x => x.Rating)
                    .ThenBy(x => x.Name)
                    .ThenBy(x => x.Id),

            ExperienceSortBy.Distance => ApplyDistanceSorting(query, request),

            _ => query
                .OrderBy(x => x.Rating == null)
                .ThenByDescending(x => x.Rating)
                .ThenByDescending(x => x.Reviews)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id)
        };
    }

    private static IOrderedQueryable<Experience> ApplyDistanceSorting(
        IQueryable<Experience> query,
        ExperienceListRequest request)
    {
        var latitude = request.CurrentLatitude!.Value;
        var longitude = request.CurrentLongitude!.Value;
        var longitudeScale = Math.Cos(latitude * Math.PI / 180d);
        var descending = request.SortDirection == ExperienceSortDirection.Desc;

        return descending
            ? query
                .OrderBy(x => x.Latitude == null || x.Longitude == null)
                .ThenByDescending(x =>
                    ((x.Latitude!.Value - latitude) * (x.Latitude.Value - latitude)) +
                    ((x.Longitude!.Value - longitude) * longitudeScale *
                     (x.Longitude.Value - longitude) * longitudeScale))
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id)
            : query
                .OrderBy(x => x.Latitude == null || x.Longitude == null)
                .ThenBy(x =>
                    ((x.Latitude!.Value - latitude) * (x.Latitude.Value - latitude)) +
                    ((x.Longitude!.Value - longitude) * longitudeScale *
                     (x.Longitude.Value - longitude) * longitudeScale))
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id);
    }

    private static void ValidateListRequest(ExperienceListRequest request)
    {
        if (!Enum.IsDefined(request.SortBy) || !Enum.IsDefined(request.SortDirection))
        {
            throw new ValidationException("A valid experience sort and direction are required.");
        }

        var hasLatitude = request.CurrentLatitude is not null;
        var hasLongitude = request.CurrentLongitude is not null;
        if (hasLatitude != hasLongitude)
        {
            throw new ValidationException("Current latitude and longitude must be provided together.");
        }

        if (request.CurrentLatitude is < -90 or > 90 ||
            request.CurrentLongitude is < -180 or > 180)
        {
            throw new ValidationException("Current latitude or longitude is outside its valid range.");
        }

        if (request.SortBy == ExperienceSortBy.Distance && (!hasLatitude || !hasLongitude))
        {
            throw new ValidationException("Current coordinates are required for distance sorting.");
        }
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
        _dbContext.ExperienceReviews.RemoveRange(
            experience.ExperienceReviews.Where(x => x.SourceList != "glinter"));
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
            StartingPricePerPerson = GetStartingPricePerPerson(experience, now),
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
            CurrentInsight = BuildVisitInsight(experience, now),
            IsActive = experience.IsActive,
            CreatedAtUtc = experience.CreatedAtUtc,
            UpdatedAtUtc = experience.UpdatedAtUtc,
            ModerationStatus = experience.ModerationStatus,
            ModerationNotes = experience.ModerationNotes,
            ModeratedByUserId = experience.ModeratedByUserId,
            ModeratedAtUtc = experience.ModeratedAtUtc
        };
    }

    private static decimal? GetStartingPricePerPerson(Experience experience, DateTime now)
    {
        var nowUtc = now.Kind == DateTimeKind.Utc ? now : now.ToUniversalTime();
        return experience.AvailabilitySlots
            .Where(x => x.IsActive && x.StartTimeUtc > nowUtc)
            .Select(x => (decimal?)x.PricePerPerson)
            .Min();
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
        if (experience.Adm0Gid is not null ||
            experience.Adm1Gid is not null ||
            experience.Adm2Gid is not null ||
            experience.Adm3Gid is not null)
        {
            return;
        }

        if (experience.Latitude is null || experience.Longitude is null)
        {
            return;
        }

        var hierarchy = await _regionsPointLookupRepository.GetHierarchyByPointAsync(
            experience.Latitude.Value,
            experience.Longitude.Value,
            cancellationToken);

        if (hierarchy is not null)
        {
            experience.Adm0Gid = hierarchy.Adm0Gid;
            experience.Adm1Gid = hierarchy.Adm1Gid;
            experience.Adm2Gid = hierarchy.Adm2Gid;
            experience.Adm3Gid = hierarchy.Adm3Gid;
        }
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
