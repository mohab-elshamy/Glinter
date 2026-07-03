using Glinter.Modules.Experiences.Domain.Enums;

namespace Glinter.Modules.Experiences.Domain.Entities;

public class Experience
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

    public string? GoogleMapsLink { get; set; }
    public string? PhoneInternational { get; set; }
    public string? PriceRange { get; set; }
    public int? Reviews { get; set; }
    public decimal? Rating { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; } = true;
    public ExperienceModerationStatus ModerationStatus { get; set; } = ExperienceModerationStatus.Approved;
    public string? ModerationNotes { get; set; }
    public Guid? ModeratedByUserId { get; set; }
    public DateTime? ModeratedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public List<ExperienceFeaturedImage> FeaturedImages { get; set; } = [];
    public List<ExperienceHour> Hours { get; set; } = [];
    public List<ExperiencePopularTime> PopularTimes { get; set; } = [];
    public List<ExperienceReviewsPerRating> ReviewsPerRatings { get; set; } = [];
    public List<ExperienceAmenity> Amenities { get; set; } = [];
    public List<ExperienceReview> ExperienceReviews { get; set; } = [];
    public List<ExperienceAvailability> AvailabilitySlots { get; set; } = [];
    public List<ExperienceBooking> Bookings { get; set; } = [];
}
