namespace Glinter.Modules.Profiles.Application.Profiles.Dtos;

public sealed class AdminLocalBuddyListItemResponse
{
    public Guid UserId { get; set; }
    public Guid ProfileId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Languages { get; set; }
    public string? ProfileImageUrl { get; set; }
    public decimal Rating { get; set; }
    public int ReviewsCount { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
