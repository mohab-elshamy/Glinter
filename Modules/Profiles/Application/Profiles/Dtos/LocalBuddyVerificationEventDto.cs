namespace Glinter.Modules.Profiles.Application.Profiles.Dtos;

public sealed class LocalBuddyVerificationEventDto
{
    public Guid Id { get; set; }
    public Guid LocalBuddyUserId { get; set; }
    public Guid ActorUserId { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
