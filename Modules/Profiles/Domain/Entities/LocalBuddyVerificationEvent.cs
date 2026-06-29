using Glinter.Modules.Profiles.Domain.Enums;

namespace Glinter.Modules.Profiles.Domain.Entities;

public sealed class LocalBuddyVerificationEvent
{
    public Guid Id { get; set; }
    public Guid LocalBuddyUserId { get; set; }
    public Guid ActorUserId { get; set; }
    public VerificationStatus PreviousStatus { get; set; }
    public VerificationStatus NewStatus { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
