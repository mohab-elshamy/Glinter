namespace Glinter.Modules.Experiences.Application.Experiences.Commands.SetExperienceApprovalStatus;

public class SetExperienceApprovalStatusCommandValidator
{
    public void Validate(SetExperienceApprovalStatusCommand command)
    {
        if (command.ExperienceId == Guid.Empty)
        {
            throw new ArgumentException("ExperienceId is required.");
        }

        if (!Enum.IsDefined(command.ApprovalStatus))
            throw new ArgumentException("ApprovalStatus is invalid.");

        if (!string.IsNullOrWhiteSpace(command.ModerationNotes) &&
            command.ModerationNotes.Length > 1000)
        {
            throw new ArgumentException("ModerationNotes cannot exceed 1000 characters.");
        }
    }
}
