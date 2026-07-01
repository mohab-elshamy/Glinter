namespace Glinter.Modules.Profiles.Domain.Entities;

public sealed class ExperienceFavorite
{
    public Guid UserId { get; set; }
    public int ExperienceId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
