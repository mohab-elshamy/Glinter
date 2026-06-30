namespace Glinter.Modules.Profiles.Application.Profiles.Dtos;

public class FollowStatusResponse
{
    public Guid FollowedUserId { get; set; }

    public bool IsFollowing { get; set; }

    public int FollowersCount { get; set; }
}
