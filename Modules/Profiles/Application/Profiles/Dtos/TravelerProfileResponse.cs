namespace Glinter.Modules.Profiles.Application.Profiles.Dtos;

public class TravelerProfileResponse
{
    public Guid ProfileId { get; set; }

    public Guid UserId { get; set; }

    public string ProfileType { get; set; } = "Traveler";

    public string DisplayName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public string? Nationality { get; set; }

    public string? PreferredBudgetLevel { get; set; }

    public string? TravelStyle { get; set; }

    public string? PreferredInterests { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
    
    public int FollowersCount { get; set; }

    public int FollowingCount { get; set; }
}