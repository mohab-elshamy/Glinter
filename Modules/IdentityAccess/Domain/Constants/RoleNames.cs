namespace Glinter.Modules.IdentityAccess.Domain.Constants;

public static class RoleNames
{
    public const string Traveler = "Traveler";
    public const string LocalBuddy = "LocalBuddy";
    public const string HotelOwner = "HotelOwner";
    public const string ExperienceProvider = "ExperienceProvider";
    public const string Admin = "Admin";

    public static readonly string[] All =
    [
        Traveler,
        LocalBuddy,
        HotelOwner,
        ExperienceProvider,
        Admin
    ];
}