namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public sealed class ExperienceModerationEventDto
{
    public Guid Id { get; set; }
    public Guid ExperienceId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
