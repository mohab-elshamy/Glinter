namespace Glinter.Modules.Profiles.Application.Profiles.Dtos;

public class LocalBuddyListItemResponse
{
    public Guid ProfileId { get; set; }

    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public string City { get; set; } = string.Empty;

    public string? Languages { get; set; }

    public decimal Rating { get; set; }

    public int ReviewsCount { get; set; }

    public string VerificationStatus { get; set; } = string.Empty;

    public List<InterestResponse> Interests { get; set; } = [];
}