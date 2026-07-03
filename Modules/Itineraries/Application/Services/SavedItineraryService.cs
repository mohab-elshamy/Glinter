using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Itineraries.Application.Dtos;
using Glinter.Modules.Itineraries.Domain.Entities;
using Glinter.Modules.Itineraries.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Glinter.Modules.Itineraries.Application.Services;

public sealed class SavedItineraryService(
    ItinerariesDbContext dbContext,
    ICurrentUserService currentUser)
{
    public async Task<SavedItineraryResponse> CreateAsync(
        SaveItineraryRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        var entity = new SavedItinerary
        {
            UserId = RequireUserId(),
            Title = request.Title.Trim(),
            Destination = request.Destination?.Trim(),
            Adm0Gid = request.Adm0Gid,
            Adm1Gid = request.Adm1Gid,
            Adm2Gid = request.Adm2Gid,
            Adm3Gid = request.Adm3Gid,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PreferredLanguage = NormalizeLanguage(request.PreferredLanguage),
            EstimatedTotalCost = request.EstimatedTotalCost,
            Currency = request.Currency?.Trim().ToUpperInvariant(),
            PlannerExplanation = request.PlannerExplanation?.Trim(),
            WarningsJson = JsonSerializer.Serialize(request.Warnings),
            RecommendationScore = request.RecommendationScore,
            TotalDistanceKm = request.TotalDistanceKm,
            TotalTravelMinutes = request.TotalTravelMinutes,
            Pace = request.Pace,
            TravelMode = request.TravelMode,
            FallbackTravelMode = request.FallbackTravelMode,
            OriginLatitude = request.Origin?.Latitude,
            OriginLongitude = request.Origin?.Longitude,
            OriginLabel = request.Origin?.Label,
            WeatherLatitude = request.WeatherLatitude,
            WeatherLongitude = request.WeatherLongitude,
            WeatherLocation = request.WeatherLocation,
            Items = request.Items.Select(ToEntity).ToList()
        };
        dbContext.Itineraries.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<List<SavedItineraryResponse>> ListAsync(
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var entities = await dbContext.Itineraries
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Include(x => x.Items)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(100)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        return entities.Select(ToResponse).ToList();
    }

    public async Task<SavedItineraryResponse?> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await FindOwnedAsync(id, false, cancellationToken);
        return entity is null ? null : ToResponse(entity);
    }

    public async Task<SavedItineraryResponse?> UpdateAsync(
        Guid id,
        UpdateSavedItineraryRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await FindOwnedAsync(id, true, cancellationToken);
        if (entity is null)
        {
            return null;
        }
        EnsureCurrent(entity, request.ExpectedUpdatedAtUtc);
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 160)
        {
            throw new ArgumentException("Title is required and cannot exceed 160 characters.");
        }

        entity.Title = request.Title.Trim();
        entity.Destination = request.Destination?.Trim();
        entity.EstimatedTotalCost = request.EstimatedTotalCost;
        entity.Currency = request.Currency?.Trim().ToUpperInvariant();
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<SavedItineraryResponse?> ReplaceItemsAsync(
        Guid id,
        ReplaceItineraryItemsRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await FindOwnedAsync(id, true, cancellationToken);
        if (entity is null)
        {
            return null;
        }
        EnsureCurrent(entity, request.ExpectedUpdatedAtUtc);
        ValidateItems(request.Items, entity.StartDate, entity.EndDate);

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        dbContext.ItineraryItems.RemoveRange(entity.Items);
        entity.Items.Clear();
        foreach (var item in request.Items)
        {
            entity.Items.Add(ToEntity(item));
        }
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindOwnedAsync(id, true, cancellationToken);
        if (entity is null)
        {
            return false;
        }
        dbContext.Itineraries.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<SavedItinerary?> FindOwnedAsync(
        Guid id,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        IQueryable<SavedItinerary> query = dbContext.Itineraries;
        if (!tracking)
        {
            query = query.AsNoTracking();
        }
        return await query
            .Where(x => x.Id == id && x.UserId == userId)
            .Include(x => x.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void Validate(SaveItineraryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 160)
        {
            throw new ArgumentException("Title is required and cannot exceed 160 characters.");
        }
        if (request.EndDate < request.StartDate)
        {
            throw new ArgumentException("End date cannot be before start date.");
        }
        if ((request.EndDate.DayNumber - request.StartDate.DayNumber) > 30)
        {
            throw new ArgumentException("An itinerary cannot exceed 31 days.");
        }
        ValidateItems(request.Items, request.StartDate, request.EndDate);
    }

    private static void ValidateItems(
        IReadOnlyList<SaveItineraryItemRequest> items,
        DateOnly startDate,
        DateOnly endDate)
    {
        var dayCount = endDate.DayNumber - startDate.DayNumber + 1;
        if (items.Count > 300)
        {
            throw new ArgumentException("An itinerary cannot contain more than 300 items.");
        }
        if (items.Any(x => x.DayNumber < 1 || x.DayNumber > dayCount ||
                           x.SortOrder < 1 ||
                           string.IsNullOrWhiteSpace(x.Name) ||
                           x.Latitude is < -90 or > 90 ||
                           x.Longitude is < -180 or > 180 ||
                           (x.EndTime is not null && x.StartTime is not null &&
                            x.EndTime <= x.StartTime)))
        {
            throw new ArgumentException("One or more itinerary items are invalid.");
        }
        if (items.GroupBy(x => new { x.DayNumber, x.SortOrder }).Any(x => x.Count() > 1))
        {
            throw new ArgumentException("Each item must have a unique position within its day.");
        }
    }

    private static SavedItineraryItem ToEntity(SaveItineraryItemRequest request) =>
        new()
        {
            Id = request.Id ?? Guid.NewGuid(),
            DayNumber = request.DayNumber,
            SortOrder = request.SortOrder,
            EntityType = request.EntityType.Trim(),
            EntityId = request.EntityId,
            NameSnapshot = request.Name.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            EstimatedCost = request.EstimatedCost,
            Explanation = request.Explanation?.Trim()
            ,Category = request.Category
            ,Rating = request.Rating
            ,ImageUrl = request.ImageUrl
            ,TravelModeFromPrevious = request.TravelModeFromPrevious
            ,RouteProviderFromPrevious = request.RouteProviderFromPrevious
            ,RouteGeometryJson = request.RouteGeometryFromPrevious is null
                ? null
                : JsonSerializer.Serialize(request.RouteGeometryFromPrevious)
            ,RouteInstructionsJson = JsonSerializer.Serialize(request.RouteInstructionsFromPrevious)
            ,RouteWarningsJson = JsonSerializer.Serialize(request.RouteWarningsFromPrevious)
            ,DistanceKmFromPrevious = request.DistanceKmFromPrevious
            ,TravelDurationMinutesFromPrevious = request.TravelDurationMinutesFromPrevious
        };

    private static SavedItineraryResponse ToResponse(SavedItinerary entity) =>
        new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Destination = entity.Destination,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            PreferredLanguage = entity.PreferredLanguage,
            EstimatedTotalCost = entity.EstimatedTotalCost,
            Currency = entity.Currency,
            Adm0Gid = entity.Adm0Gid,
            Adm1Gid = entity.Adm1Gid,
            Adm2Gid = entity.Adm2Gid,
            Adm3Gid = entity.Adm3Gid,
            PlannerExplanation = entity.PlannerExplanation,
            Warnings = Deserialize<List<string>>(entity.WarningsJson) ?? [],
            RecommendationScore = entity.RecommendationScore,
            TotalDistanceKm = entity.TotalDistanceKm,
            TotalTravelMinutes = entity.TotalTravelMinutes,
            Pace = entity.Pace,
            TravelMode = entity.TravelMode,
            FallbackTravelMode = entity.FallbackTravelMode,
            Origin = entity.OriginLatitude is null || entity.OriginLongitude is null
                ? null
                : new ItineraryPointRequest
                {
                    Latitude = entity.OriginLatitude.Value,
                    Longitude = entity.OriginLongitude.Value,
                    Label = entity.OriginLabel
                },
            WeatherLatitude = entity.WeatherLatitude,
            WeatherLongitude = entity.WeatherLongitude,
            WeatherLocation = entity.WeatherLocation,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            Items = entity.Items
                .OrderBy(x => x.DayNumber)
                .ThenBy(x => x.SortOrder)
                .Select(x => new SavedItineraryItemResponse
                {
                    Id = x.Id,
                    DayNumber = x.DayNumber,
                    SortOrder = x.SortOrder,
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    Name = x.NameSnapshot,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    EstimatedDurationMinutes = x.EstimatedDurationMinutes,
                    EstimatedCost = x.EstimatedCost,
                    Explanation = x.Explanation,
                    Category = x.Category,
                    Rating = x.Rating,
                    ImageUrl = x.ImageUrl,
                    TravelModeFromPrevious = x.TravelModeFromPrevious,
                    RouteProviderFromPrevious = x.RouteProviderFromPrevious,
                    RouteGeometryFromPrevious = Deserialize<ItineraryLegGeometryResponse>(x.RouteGeometryJson),
                    RouteInstructionsFromPrevious = Deserialize<List<string>>(x.RouteInstructionsJson) ?? [],
                    RouteWarningsFromPrevious = Deserialize<List<string>>(x.RouteWarningsJson) ?? [],
                    DistanceKmFromPrevious = x.DistanceKmFromPrevious,
                    TravelDurationMinutesFromPrevious = x.TravelDurationMinutesFromPrevious
                })
                .ToList()
        };

    private static T? Deserialize<T>(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return default;
        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static void EnsureCurrent(
        SavedItinerary entity,
        DateTime expectedUpdatedAtUtc)
    {
        if (expectedUpdatedAtUtc == default ||
            entity.UpdatedAtUtc != expectedUpdatedAtUtc)
        {
            throw new DbUpdateConcurrencyException(
                "The itinerary changed after it was loaded. Refresh and try again.");
        }
    }

    private Guid RequireUserId() =>
        currentUser.UserId
        ?? throw new InvalidOperationException("Authenticated user id is missing.");

    private static string NormalizeLanguage(string? language) =>
        language?.Trim().StartsWith("ar", StringComparison.OrdinalIgnoreCase) == true
            ? "ar"
            : "en";
}
