namespace Glinter.Modules.Buddy.Application.Reviews.Dtos;

public sealed class CreateBuddyReviewRequest
{
    public int Rating { get; set; }
    public string ReviewText { get; set; } = string.Empty;
}

public sealed class LegacyCreateBuddyReviewRequest
{
    public Guid BookingId { get; set; }
    public int Rating { get; set; }
    public string ReviewText { get; set; } = string.Empty;
}

public sealed class BuddyReviewResponse
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid BookingId => RequestId;
    public Guid LocalBuddyUserId { get; set; }
    public Guid TravelerUserId { get; set; }
    public string TravelerDisplayName { get; set; } = string.Empty;
    public string ReviewerName => TravelerDisplayName;
    public int Rating { get; set; }
    public string ReviewText { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class BuddyReviewSummaryResponse
{
    public decimal AverageRating { get; set; }
    public int ReviewsCount { get; set; }
}
