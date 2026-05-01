namespace Glinter.Modules.Profiles.Domain.Entities;

public class TravelerProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public string? Nationality { get; set; }

    public string? PreferredBudgetLevel { get; set; }

    public string? TravelStyle { get; set; }

    public string? PreferredInterests { get; set; }

    public string? ProfileImageUrl { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<TravelerInterest> Interests { get; set; } = new List<TravelerInterest>();
}