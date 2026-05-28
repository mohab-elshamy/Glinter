using Glinter.Modules.Experiences.Domain.Enums;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.SetExperienceApprovalStatus;

public class SetExperienceApprovalStatusCommand
{
    public Guid ExperienceId { get; set; }

    public ExperienceApprovalStatus ApprovalStatus { get; set; }

    public string? ModerationNotes { get; set; }
}