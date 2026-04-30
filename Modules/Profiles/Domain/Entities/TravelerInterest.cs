namespace Glinter.Modules.Profiles.Domain.Entities;

public class TravelerInterest
{
    public Guid TravelerProfileId { get; set; }

    public Guid InterestId { get; set; }

    public TravelerProfile TravelerProfile { get; set; } = null!;

    public Interest Interest { get; set; } = null!;
}