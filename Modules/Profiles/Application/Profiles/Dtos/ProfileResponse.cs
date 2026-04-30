namespace Glinter.Modules.Profiles.Application.Profiles.Dtos;

public class ProfileResponse
{
    public Guid ProfileId { get; set; }

    public Guid UserId { get; set; }

    public string ProfileType { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Bio { get; set; }

    public string? Nationality { get; set; }

    public string? PreferredBudgetLevel { get; set; }

    public string? TravelStyle { get; set; }

    public string? PreferredInterests { get; set; }

    public string? City { get; set; }

    public string? Languages { get; set; }

    public decimal? Rating { get; set; }

    public int? ReviewsCount { get; set; }

    public string? VerificationStatus { get; set; }

    public string? BusinessName { get; set; }

    public string? ContactPersonName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Description { get; set; }

    public List<InterestResponse> Interests { get; set; } = [];

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}