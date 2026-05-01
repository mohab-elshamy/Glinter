namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertHotelOwnerProfile;

public class UpsertHotelOwnerProfileCommand
{
    public string BusinessName { get; set; } = string.Empty;

    public string? ContactPersonName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Description { get; set; }
}