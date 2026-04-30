namespace Glinter.Modules.Profiles.Domain.Entities;

public class UserFollow
{
    public Guid FollowerUserId { get; set; }

    public Guid FollowedUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}