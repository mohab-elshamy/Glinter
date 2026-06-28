using Glinter.Modules.Experiences.Domain.Enums;

namespace Glinter.Modules.Experiences.Domain.Entities;

public sealed class ExperienceModerationEvent
{
    public Guid Id { get; set; }
    public Guid ExperienceId { get; set; }
    public Experience Experience { get; set; } = null!;
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public ExperienceApprovalStatus PreviousStatus { get; set; }
    public ExperienceApprovalStatus NewStatus { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
