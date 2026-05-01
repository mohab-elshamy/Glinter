namespace Glinter.Modules.Profiles.Application.Profiles.Dtos;

public class HotelOwnerProfileResponse
{
    public Guid ProfileId { get; set; }

    public Guid UserId { get; set; }

    public string ProfileType { get; set; } = "HotelOwner";

    public string BusinessName { get; set; } = string.Empty;

    public string? ContactPersonName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Description { get; set; }
    
    public string? ProfileImageUrl { get; set; }

    public int FollowersCount { get; set; }

    public int FollowingCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}