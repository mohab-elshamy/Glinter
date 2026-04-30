using Glinter.Modules.Profiles.Domain.Enums;

namespace Glinter.Modules.Profiles.Domain.Entities;

public class LocalBuddyProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public string City { get; set; } = string.Empty;

    public string? Languages { get; set; }

    public decimal Rating { get; set; }

    public int ReviewsCount { get; set; }

    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<BuddyInterest> Interests { get; set; } = new List<BuddyInterest>();
}