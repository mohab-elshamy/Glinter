namespace Glinter.Modules.Profiles.Application.Profiles.Dtos;

public class TravelerProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public string? Nationality { get; set; }

    public string? PreferredBudgetLevel { get; set; }

    public string? TravelStyle { get; set; }

    public string? PreferredInterests { get; set; }
    public string? PreferredVibes { get; set; }
    public string? ComfortLevel { get; set; }
    public string? SafetyPriority { get; set; }

    public List<Guid> InterestIds { get; set; } = [];
}
