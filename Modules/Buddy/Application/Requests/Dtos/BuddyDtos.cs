namespace Glinter.Modules.Buddy.Application.Requests.Dtos;

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

public sealed class CreateBuddyRequest
{
    public Guid LocalBuddyUserId { get; set; }
    public Guid AvailabilityId { get; set; }
    public string? Notes { get; set; }
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

public sealed class BuddyRequestResponse
{
    public Guid Id { get; set; }
    public Guid AvailabilityId { get; set; }
    public Guid LocalBuddyUserId { get; set; }
    public string LocalBuddyDisplayName { get; set; } = string.Empty;
    public string BuddyName => LocalBuddyDisplayName;
    public Guid TravelerUserId { get; set; }
    public string TravelerDisplayName { get; set; } = string.Empty;
    public string TravelerName => TravelerDisplayName;
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public bool CanCancel { get; set; }
    public bool CanReview { get; set; }
    public bool HasReview { get; set; }
}
