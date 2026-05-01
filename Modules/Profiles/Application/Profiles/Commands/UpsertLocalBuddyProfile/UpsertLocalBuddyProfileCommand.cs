namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertLocalBuddyProfile;

public class UpsertLocalBuddyProfileCommand
{
    public string DisplayName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public string City { get; set; } = string.Empty;

    public string? Languages { get; set; }

    public List<Guid> InterestIds { get; set; } = [];
}