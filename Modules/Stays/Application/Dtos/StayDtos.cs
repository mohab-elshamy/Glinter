using Glinter.Modules.Stays.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Glinter.Modules.Stays.Application.Dtos;

public class CreateStayRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string? Description { get; set; }
    public string? GoogleMapsLink { get; set; }
    public string? Website { get; set; }
    public string? PhoneInternational { get; set; }
    public string? LocationSummaryDescription { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public List<string> ImageLinks { get; set; } = [];
    public List<string> Amenities { get; set; } = [];
    public List<StayBookingPlatformRequest> BookingPlatforms { get; set; } = [];
}

public class UpdateStayRequest : CreateStayRequest
{
}

public class StayBookingPlatformRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal? PriceWithTax { get; set; }
    public string? Link { get; set; }
}

public class ImportStaysFileRequest
{
    public IFormFile? File { get; set; }
}

public class UploadStayImageRequest
{
    public IFormFile? File { get; set; }
}

public class StayImageUploadResponse
{
    public string Link { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}

public class StayListRequest
{
    public StaySourceType? SourceType { get; set; }
    public string? Search { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinRating { get; set; }
    public StaySortBy SortBy { get; set; } = StaySortBy.Recommended;
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class StayFavoriteListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class StayFavoriteStatusResponse
{
    public int StayId { get; set; }
    public bool IsFavorite { get; set; }
}

public class StayFavoriteSummaryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public decimal? Rating { get; set; }
    public int? Reviews { get; set; }
    public string? PrimaryImage { get; set; }
    public string? LocationSummaryDescription { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime FavoritedAtUtc { get; set; }
}

public class CreateStayBookingRequest
{
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int GuestCount { get; set; }
}

public class UpdateStayBookingStatusRequest
{
    public StayBookingStatus Status { get; set; }
}

public class StayBookingResponse
{
    public Guid Id { get; set; }
    public int StayId { get; set; }
    public string StayName { get; set; } = string.Empty;
    public Guid TravelerProfileId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int GuestCount { get; set; }
    public decimal TotalPrice { get; set; }
    public StayBookingStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public class StayRegionStatsRequest
{
    public StayRegionGroupBy GroupBy { get; set; } = StayRegionGroupBy.Adm3;
    public StaySourceType? SourceType { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinRating { get; set; }
}

public class StayRegionStatsResponse
{
    public StayRegionGroupBy GroupBy { get; set; }
    public int RegionGid { get; set; }
    public int HotelsCount { get; set; }
    public decimal? AveragePrice { get; set; }
    public decimal? PricePercentage { get; set; }
}

public class StayResponse
{
    public int Id { get; set; }
    public StaySourceType SourceType { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? HotelOwnerProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string? Description { get; set; }
    public string? GoogleMapsLink { get; set; }
    public int? Reviews { get; set; }
    public decimal? Rating { get; set; }
    public string? Website { get; set; }
    public string? PhoneInternational { get; set; }
    public string? LocationSummaryDescription { get; set; }
    public string? Cid { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public List<StayImageResponse> Images { get; set; } = [];
    public List<string> Amenities { get; set; } = [];
    public List<StayReviewsPerRatingResponse> ReviewsPerRating { get; set; } = [];
    public List<StayBookingPlatformResponse> BookingPlatforms { get; set; } = [];
    public List<StayReviewResponse> FeaturedReviews { get; set; } = [];
}

public class StayImageResponse
{
    public int Id { get; set; }
    public string Link { get; set; } = string.Empty;
}

public class StayReviewsPerRatingResponse
{
    public int Rating { get; set; }
    public int ReviewsCount { get; set; }
}

public class StayBookingPlatformResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? PriceWithTax { get; set; }
    public string? Link { get; set; }
}

public class CreateStayReviewRequest
{
    public int Rating { get; set; }
    public string ReviewText { get; set; } = string.Empty;
}

public class StayReviewResponse
{
    public int Id { get; set; }
    public string? ExternalReviewId { get; set; }
    public string? ReviewerName { get; set; }
    public int? Rating { get; set; }
    public string? ReviewText { get; set; }
    public string Platform { get; set; } = string.Empty;
    public DateTime? PublishedAtDate { get; set; }
}

public class StayReviewsForLlmResponse
{
    public int StayId { get; set; }
    public string StayName { get; set; } = string.Empty;
    public int ReviewsCount { get; set; }
    public string ReviewsText { get; set; } = string.Empty;
}

public class ImportStaysResponse
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
}

public class PagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<T> Items { get; set; } = [];
}
