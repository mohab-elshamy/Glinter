namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertTravelerProfile;

public class UpsertTravelerProfileCommand
{
    public string DisplayName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public string? Nationality { get; set; }

    public string? PreferredBudgetLevel { get; set; }

    public string? TravelStyle { get; set; }

    public string? PreferredInterests { get; set; }
}