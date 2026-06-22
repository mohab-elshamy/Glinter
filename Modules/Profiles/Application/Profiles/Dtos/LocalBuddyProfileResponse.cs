namespace Glinter.Modules.Profiles.Application.Profiles.Dtos;

public class LocalBuddyProfileResponse
{
    public Guid ProfileId { get; set; }

    public Guid UserId { get; set; }

    public string ProfileType { get; set; } = "LocalBuddy";

    public string DisplayName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public string City { get; set; } = string.Empty;

    public string? Languages { get; set; }

    public string? ProfileImageUrl { get; set; }

    public decimal Rating { get; set; }

    public int ReviewsCount { get; set; }

    public string VerificationStatus { get; set; } = string.Empty;

    public List<InterestResponse> Interests { get; set; } = [];

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public int FollowersCount { get; set; }

    public int FollowingCount { get; set; }
}