namespace Glinter.Modules.Profiles.Domain.Entities;

public class BuddyInterest
{
    public Guid LocalBuddyProfileId { get; set; }

    public Guid InterestId { get; set; }

    public LocalBuddyProfile LocalBuddyProfile { get; set; } = null!;

    public Interest Interest { get; set; } = null!;
}