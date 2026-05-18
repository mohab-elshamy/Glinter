namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class SetExperienceApprovalStatusRequestDto
{
    public string ApprovalStatus { get; set; } = string.Empty;

    public string? ModerationNotes { get; set; }
}