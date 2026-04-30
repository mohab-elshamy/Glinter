using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Glinter.Modules.Profiles.Domain.Entities;

namespace Glinter.Modules.Profiles.Application.Common.Mapping;

public static class ProfilesMappings
{
    public static ProfileResponse ToProfileResponse(TravelerProfile profile)
    {
        return new ProfileResponse
        {
            ProfileId = profile.Id,
            UserId = profile.UserId,
            ProfileType = "Traveler",
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            Nationality = profile.Nationality,
            PreferredBudgetLevel = profile.PreferredBudgetLevel,
            TravelStyle = profile.TravelStyle,
            PreferredInterests = profile.PreferredInterests,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc
        };
    }

    public static ProfileResponse ToProfileResponse(LocalBuddyProfile profile)
    {
        return new ProfileResponse
        {
            ProfileId = profile.Id,
            UserId = profile.UserId,
            ProfileType = "LocalBuddy",
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            City = profile.City,
            Languages = profile.Languages,
            Rating = profile.Rating,
            ReviewsCount = profile.ReviewsCount,
            VerificationStatus = profile.VerificationStatus.ToString(),
            Interests = profile.Interests
                .Select(x => ToInterestResponse(x.Interest))
                .ToList(),
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc
        };
    }

    public static ProfileResponse ToProfileResponse(HotelOwnerProfile profile)
    {
        return new ProfileResponse
        {
            ProfileId = profile.Id,
            UserId = profile.UserId,
            ProfileType = "HotelOwner",
            BusinessName = profile.BusinessName,
            ContactPersonName = profile.ContactPersonName,
            PhoneNumber = profile.PhoneNumber,
            Description = profile.Description,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc
        };
    }

    public static ProfileResponse ToProfileResponse(ExperienceProviderProfile profile)
    {
        return new ProfileResponse
        {
            ProfileId = profile.Id,
            UserId = profile.UserId,
            ProfileType = "ExperienceProvider",
            BusinessName = profile.BusinessName,
            ContactPersonName = profile.ContactPersonName,
            PhoneNumber = profile.PhoneNumber,
            Description = profile.Description,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc
        };
    }

    public static InterestResponse ToInterestResponse(Interest interest)
    {
        return new InterestResponse
        {
            Id = interest.Id,
            Name = interest.Name
        };
    }

    public static LocalBuddyListItemResponse ToLocalBuddyListItemResponse(LocalBuddyProfile profile)
    {
        return new LocalBuddyListItemResponse
        {
            ProfileId = profile.Id,
            UserId = profile.UserId,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            City = profile.City,
            Languages = profile.Languages,
            Rating = profile.Rating,
            ReviewsCount = profile.ReviewsCount,
            VerificationStatus = profile.VerificationStatus.ToString(),
            Interests = profile.Interests
                .Select(x => ToInterestResponse(x.Interest))
                .ToList()
        };
    }
}