using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Glinter.Modules.Profiles.Domain.Entities;

namespace Glinter.Modules.Profiles.Application.Common.Mapping;

public static class ProfilesMappings
{
    public static TravelerProfileResponse ToTravelerProfileResponse(
        TravelerProfile profile,
        int followersCount = 0,
        int followingCount = 0)
    {
        return new TravelerProfileResponse
        {
            ProfileId = profile.Id,
            UserId = profile.UserId,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            Nationality = profile.Nationality,
            PreferredBudgetLevel = profile.PreferredBudgetLevel,
            TravelStyle = profile.TravelStyle,
            PreferredInterests = profile.PreferredInterests,
            ProfileImageUrl = profile.ProfileImageUrl,
            Interests = profile.Interests
                .Select(x => ToInterestResponse(x.Interest))
                .ToList(),
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc
        };
    }

    public static LocalBuddyProfileResponse ToLocalBuddyProfileResponse(
        LocalBuddyProfile profile,
        int followersCount = 0,
        int followingCount = 0,
        bool isFollowing = false)
    {
        return new LocalBuddyProfileResponse
        {
            ProfileId = profile.Id,
            UserId = profile.UserId,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            City = profile.City,
            Languages = profile.Languages,
            ProfileImageUrl = profile.ProfileImageUrl,
            Rating = profile.Rating,
            ReviewsCount = profile.ReviewsCount,
            VerificationStatus = profile.VerificationStatus.ToString(),
            Interests = profile.Interests
                .Select(x => ToInterestResponse(x.Interest))
                .ToList(),
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            IsFollowing = isFollowing,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc
        };
    }

    public static HotelOwnerProfileResponse ToHotelOwnerProfileResponse(
        HotelOwnerProfile profile,
        int followersCount = 0,
        int followingCount = 0)
    {
        return new HotelOwnerProfileResponse
        {
            ProfileId = profile.Id,
            UserId = profile.UserId,
            BusinessName = profile.BusinessName,
            ContactPersonName = profile.ContactPersonName,
            PhoneNumber = profile.PhoneNumber,
            Description = profile.Description,
            ProfileImageUrl = profile.ProfileImageUrl,
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc
        };
    }

    public static ExperienceProviderProfileResponse ToExperienceProviderProfileResponse(
        ExperienceProviderProfile profile,
        int followersCount = 0,
        int followingCount = 0)
    {
        return new ExperienceProviderProfileResponse
        {
            ProfileId = profile.Id,
            UserId = profile.UserId,
            BusinessName = profile.BusinessName,
            ContactPersonName = profile.ContactPersonName,
            PhoneNumber = profile.PhoneNumber,
            Description = profile.Description,
            ProfileImageUrl = profile.ProfileImageUrl,
            FollowersCount = followersCount,
            FollowingCount = followingCount,
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

    public static LocalBuddyListItemResponse ToLocalBuddyListItemResponse(
        LocalBuddyProfile profile,
        int followersCount = 0,
        int followingCount = 0,
        bool isFollowing = false)
    {
        return new LocalBuddyListItemResponse
        {
            ProfileId = profile.Id,
            UserId = profile.UserId,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            City = profile.City,
            Languages = profile.Languages,
            ProfileImageUrl = profile.ProfileImageUrl,
            Rating = profile.Rating,
            ReviewsCount = profile.ReviewsCount,
            VerificationStatus = profile.VerificationStatus.ToString(),
            Interests = profile.Interests
                .Select(x => ToInterestResponse(x.Interest))
                .ToList(),
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            IsFollowing = isFollowing
        };
    }
}
