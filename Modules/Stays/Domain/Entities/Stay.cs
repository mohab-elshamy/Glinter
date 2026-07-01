using Glinter.Modules.Stays.Domain.Enums;

namespace Glinter.Modules.Stays.Domain.Entities;

public class Stay
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
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public List<StayImage> Images { get; set; } = [];
    public List<StayAmenity> Amenities { get; set; } = [];
    public List<StayReviewsPerRating> ReviewsPerRatings { get; set; } = [];
    public List<StayBookingPlatform> BookingPlatforms { get; set; } = [];
    public List<StayReview> StayReviews { get; set; } = [];
    public List<StayBooking> Bookings { get; set; } = [];
}
