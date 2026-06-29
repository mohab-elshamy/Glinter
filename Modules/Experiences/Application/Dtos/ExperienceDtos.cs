using Glinter.Modules.Experiences.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Glinter.Modules.Experiences.Application.Dtos;

public class CreateExperienceRequest
{
    public ExperienceCategory Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<string> FeaturedImageLinks { get; set; } = [];
    public List<ExperienceHourRequest> Hours { get; set; } = [];
    public string? GoogleMapsLink { get; set; }
    public List<ExperiencePopularTimeRequest> PopularTimes { get; set; } = [];
    public string? PhoneInternational { get; set; }
    public string? PriceRange { get; set; }
    public string? Website { get; set; }
    public List<string> Amenities { get; set; } = [];
}

public class ExperienceHourRequest
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }
}

public class ExperiencePopularTimeRequest
{
    public DayOfWeek DayOfWeek { get; set; }
    public int HourOfDay { get; set; }
    public int PopularityPercentage { get; set; }
}

public class ImportExperiencesFileRequest
{
    public ExperienceCategory? Category { get; set; }
    public IFormFile? File { get; set; }
}

public class ExperienceListRequest
{
    public ExperienceCategory? Category { get; set; }
    public ExperienceSourceType? SourceType { get; set; }
    public string? Search { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public decimal? MinRating { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ExperienceResponse
{
    public int Id { get; set; }
    public ExperienceCategory Category { get; set; }
    public ExperienceSourceType SourceType { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? ProviderProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Cid { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<ExperienceFeaturedImageResponse> FeaturedImages { get; set; } = [];
    public List<ExperienceHourResponse> Hours { get; set; } = [];
    public string? GoogleMapsLink { get; set; }
    public List<ExperiencePopularTimeResponse> PopularTimes { get; set; } = [];
    public string? PhoneInternational { get; set; }
    public string? PriceRange { get; set; }
    public int? Reviews { get; set; }
    public decimal? Rating { get; set; }
    public List<ExperienceReviewsPerRatingResponse> ReviewsPerRating { get; set; } = [];
    public string? Website { get; set; }
    public List<string> Amenities { get; set; } = [];
    public List<ExperienceReviewResponse> FeaturedReviews { get; set; } = [];
    public VisitInsightResponse? CurrentInsight { get; set; }
}

public class ExperienceFeaturedImageResponse
{
    public int Id { get; set; }
    public string Link { get; set; } = string.Empty;
}

public class ExperienceHourResponse
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }
}

public class ExperiencePopularTimeResponse
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public int HourOfDay { get; set; }
    public int PopularityPercentage { get; set; }
}

public class ExperienceReviewsPerRatingResponse
{
    public int Rating { get; set; }
    public int ReviewsCount { get; set; }
}

public class ExperienceMapItemResponse
{
    public int Id { get; set; }
    public ExperienceCategory Category { get; set; }
    public ExperienceSourceType SourceType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public decimal? Rating { get; set; }
    public int? Reviews { get; set; }
    public string? PrimaryImage { get; set; }
    public bool? IsOpenNow { get; set; }
    public int? PopularityPercentageNow { get; set; }
}

public class ExperienceReviewResponse
{
    public int Id { get; set; }
    public string? ExternalReviewId { get; set; }
    public string? ReviewerName { get; set; }
    public int? Rating { get; set; }
    public string? ReviewText { get; set; }
    public DateTime? PublishedAtDate { get; set; }
    public string SourceList { get; set; } = string.Empty;
}

public class ExperienceReviewsForLlmResponse
{
    public int ExperienceId { get; set; }
    public string ExperienceName { get; set; } = string.Empty;
    public int ReviewsCount { get; set; }
    public string ReviewsText { get; set; } = string.Empty;
}

public class VisitInsightResponse
{
    public DateTime RequestedAt { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public int HourOfDay { get; set; }
    public bool? IsOpen { get; set; }
    public string OpenStatus { get; set; } = "unknown";
    public int? PopularityPercentage { get; set; }
    public string CrowdLevel { get; set; } = "unknown";
    public string? BestKnownOpenWindow { get; set; }
}

public class ImportExperiencesResponse
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
