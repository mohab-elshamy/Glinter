namespace Glinter.Modules.Profiles.Application.Profiles.Dtos;

public sealed class BuddyAvailabilityRequest
{
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public decimal Price { get; set; }
}

public sealed class BuddyAvailabilityResponse
{
    public Guid Id { get; set; }
    public Guid LocalBuddyUserId { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public bool IsBooked { get; set; }
}

public sealed class CreateBuddyBookingRequest
{
    public Guid AvailabilityId { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateBuddyBookingStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public sealed class BuddyBookingResponse
{
    public Guid Id { get; set; }
    public Guid AvailabilityId { get; set; }
    public Guid LocalBuddyUserId { get; set; }
    public string BuddyName { get; set; } = string.Empty;
    public Guid TravelerUserId { get; set; }
    public string TravelerName { get; set; } = string.Empty;
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class CreateBuddyReviewRequest
{
    public Guid BookingId { get; set; }
    public int Rating { get; set; }
    public string ReviewText { get; set; } = string.Empty;
}

public sealed class BuddyReviewResponse
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid LocalBuddyUserId { get; set; }
    public Guid TravelerUserId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string ReviewText { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
