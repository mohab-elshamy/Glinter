using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Profiles.Application.Profiles.Commands.FollowUser;
using Glinter.Modules.Profiles.Application.Profiles.Commands.UnfollowUser;
using Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertLocalBuddyProfile;
using Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertTravelerProfile;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Glinter.Modules.Profiles.Application.Profiles.Queries.GetMyProfile;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Glinter.Modules.Profiles.Application.Profiles.Queries.GetFollowStatus;
using Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertHotelOwnerProfile;
using Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertExperienceProviderProfile;
using Glinter.Modules.Profiles.Application.Profiles.Commands.UpdateProfileImage;
using Glinter.Modules.Profiles.Application.Profiles.Services;
using Glinter.Modules.Profiles.Infrastructure.Files;

namespace Glinter.Modules.Profiles.Presentation.Controllers;

[ApiController]
[Route("api/profiles")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProfilesController : ControllerBase
{
    private readonly UpsertTravelerProfileCommandHandler _upsertTravelerProfileCommandHandler;
    private readonly UpsertLocalBuddyProfileCommandHandler _upsertLocalBuddyProfileCommandHandler;
    private readonly GetMyProfileQueryHandler _getMyProfileQueryHandler;
    private readonly FollowUserCommandHandler _followUserCommandHandler;
    private readonly UnfollowUserCommandHandler _unfollowUserCommandHandler;
    private readonly GetFollowStatusQueryHandler _getFollowStatusQueryHandler;
    private readonly UpsertHotelOwnerProfileCommandHandler _upsertHotelOwnerProfileCommandHandler;
    private readonly UpsertExperienceProviderProfileCommandHandler _upsertExperienceProviderProfileCommandHandler;
    private readonly UpdateProfileImageCommandHandler _updateProfileImageCommandHandler;
    private readonly ProfileImageStorage _profileImageStorage;
    private readonly ExperienceFavoriteService _experienceFavoriteService;

    public ProfilesController(
        UpsertTravelerProfileCommandHandler upsertTravelerProfileCommandHandler,
        UpsertLocalBuddyProfileCommandHandler upsertLocalBuddyProfileCommandHandler,
        UpsertHotelOwnerProfileCommandHandler upsertHotelOwnerProfileCommandHandler,
        UpsertExperienceProviderProfileCommandHandler upsertExperienceProviderProfileCommandHandler,
        GetMyProfileQueryHandler getMyProfileQueryHandler,
        FollowUserCommandHandler followUserCommandHandler,
        UnfollowUserCommandHandler unfollowUserCommandHandler,
        GetFollowStatusQueryHandler getFollowStatusQueryHandler,
        UpdateProfileImageCommandHandler updateProfileImageCommandHandler,
        ProfileImageStorage profileImageStorage,
        ExperienceFavoriteService experienceFavoriteService)
    {
        _upsertTravelerProfileCommandHandler = upsertTravelerProfileCommandHandler;
        _upsertLocalBuddyProfileCommandHandler = upsertLocalBuddyProfileCommandHandler;
        _upsertHotelOwnerProfileCommandHandler = upsertHotelOwnerProfileCommandHandler;
        _upsertExperienceProviderProfileCommandHandler = upsertExperienceProviderProfileCommandHandler;
        _getMyProfileQueryHandler = getMyProfileQueryHandler;
        _followUserCommandHandler = followUserCommandHandler;
        _unfollowUserCommandHandler = unfollowUserCommandHandler;
        _getFollowStatusQueryHandler = getFollowStatusQueryHandler;
        _updateProfileImageCommandHandler = updateProfileImageCommandHandler;
        _profileImageStorage = profileImageStorage;
        _experienceFavoriteService = experienceFavoriteService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await _getMyProfileQueryHandler.HandleAsync(
            new GetMyProfileQuery(),
            cancellationToken);

        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.Traveler)]
    [HttpPut("traveler")]
    public async Task<IActionResult> UpsertTravelerProfile(
        [FromBody] TravelerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _upsertTravelerProfileCommandHandler.HandleAsync(
            new UpsertTravelerProfileCommand
            {
                DisplayName = request.DisplayName,
                Bio = request.Bio,
                Nationality = request.Nationality,
                PreferredBudgetLevel = request.PreferredBudgetLevel,
                TravelStyle = request.TravelStyle,
                PreferredInterests = request.PreferredInterests,
                PreferredVibes = request.PreferredVibes,
                ComfortLevel = request.ComfortLevel,
                SafetyPriority = request.SafetyPriority,
                InterestIds = request.InterestIds
            },
            cancellationToken);

        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.LocalBuddy)]
    [HttpPut("local-buddy")]
    public async Task<IActionResult> UpsertLocalBuddyProfile(
        [FromBody] LocalBuddyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _upsertLocalBuddyProfileCommandHandler.HandleAsync(
            new UpsertLocalBuddyProfileCommand
            {
                DisplayName = request.DisplayName,
                Bio = request.Bio,
                City = request.City,
                Languages = request.Languages,
                InterestIds = request.InterestIds
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("users/{userId:guid}/follow")]
    public async Task<IActionResult> FollowUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _followUserCommandHandler.HandleAsync(
            new FollowUserCommand
            {
                FollowedUserId = userId
            },
            cancellationToken);

        return Ok(await _getFollowStatusQueryHandler.HandleAsync(
            new GetFollowStatusQuery { FollowedUserId = userId },
            cancellationToken));
    }

    [HttpDelete("users/{userId:guid}/follow")]
    public async Task<IActionResult> UnfollowUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _unfollowUserCommandHandler.HandleAsync(
            new UnfollowUserCommand
            {
                FollowedUserId = userId
            },
            cancellationToken);

        return Ok(await _getFollowStatusQueryHandler.HandleAsync(
            new GetFollowStatusQuery { FollowedUserId = userId },
            cancellationToken));
    }

    [HttpGet("users/{userId:guid}/follow-status")]
    public async Task<IActionResult> GetFollowStatus(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _getFollowStatusQueryHandler.HandleAsync(
            new GetFollowStatusQuery
            {
                FollowedUserId = userId
            },
            cancellationToken);

        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.HotelOwner)]
    [HttpPut("hotel-owner")]
    public async Task<IActionResult> UpsertHotelOwnerProfile(
        [FromBody] HotelOwnerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _upsertHotelOwnerProfileCommandHandler.HandleAsync(
            new UpsertHotelOwnerProfileCommand
            {
                BusinessName = request.BusinessName,
                ContactPersonName = request.ContactPersonName,
                PhoneNumber = request.PhoneNumber,
                Description = request.Description
            },
            cancellationToken);

        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.ExperienceProvider)]
    [HttpPut("experience-provider")]
    public async Task<IActionResult> UpsertExperienceProviderProfile(
        [FromBody] ExperienceProviderProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _upsertExperienceProviderProfileCommandHandler.HandleAsync(
            new UpsertExperienceProviderProfileCommand
            {
                BusinessName = request.BusinessName,
                ContactPersonName = request.ContactPersonName,
                PhoneNumber = request.PhoneNumber,
                Description = request.Description
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("image")]
    public async Task<IActionResult> UpdateProfileImage(
        [FromBody] UpdateProfileImageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updateProfileImageCommandHandler.HandleAsync(
            new UpdateProfileImageCommand
            {
                ProfileImageUrl = request.ProfileImageUrl
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ProfileImageStorage.MaxImageBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProfileImageStorage.MaxImageBytes)]
    public async Task<IActionResult> UploadProfileImage(
        [FromForm] UploadProfileImageRequest request,
        CancellationToken cancellationToken)
    {
        var stored = await _profileImageStorage.SaveAsync(request.File, cancellationToken);
        return Ok(new ProfileImageUploadResponse
        {
            FileName = stored.FileName,
            SizeBytes = stored.SizeBytes,
            Link = Url.ActionLink(
                nameof(GetProfileImage),
                values: new { fileName = stored.FileName })!
        });
    }

    [AllowAnonymous]
    [HttpGet("images/{fileName}")]
    public IActionResult GetProfileImage(string fileName)
    {
        var stored = _profileImageStorage.Find(fileName);
        return stored is null
            ? NotFound()
            : PhysicalFile(stored.Value.Path, stored.Value.ContentType);
    }

    [HttpGet("experience-favorites")]
    public async Task<IActionResult> GetExperienceFavorites(CancellationToken cancellationToken) =>
        Ok(await _experienceFavoriteService.GetIdsAsync(cancellationToken));

    [HttpPut("experience-favorites/{experienceId:int}")]
    public async Task<IActionResult> AddExperienceFavorite(
        int experienceId,
        CancellationToken cancellationToken) =>
        Ok(await _experienceFavoriteService.AddAsync(experienceId, cancellationToken));

    [HttpDelete("experience-favorites/{experienceId:int}")]
    public async Task<IActionResult> RemoveExperienceFavorite(
        int experienceId,
        CancellationToken cancellationToken) =>
        Ok(await _experienceFavoriteService.RemoveAsync(experienceId, cancellationToken));

    [HttpDelete("experience-favorites")]
    public async Task<IActionResult> ClearExperienceFavorites(CancellationToken cancellationToken)
    {
        await _experienceFavoriteService.ClearAsync(cancellationToken);
        return NoContent();
    }
}

public sealed class UploadProfileImageRequest
{
    public IFormFile? File { get; set; }
}

public sealed class ProfileImageUploadResponse
{
    public string Link { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}
