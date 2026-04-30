namespace Glinter.Modules.Profiles.Domain.Entities;

public class Interest
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<BuddyInterest> BuddyInterests { get; set; } = new List<BuddyInterest>();
}