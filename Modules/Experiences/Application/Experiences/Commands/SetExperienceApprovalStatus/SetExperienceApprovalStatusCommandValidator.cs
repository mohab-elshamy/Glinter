namespace Glinter.Modules.Experiences.Application.Experiences.Commands.SetExperienceApprovalStatus;

public class SetExperienceApprovalStatusCommandValidator
{
    public void Validate(SetExperienceApprovalStatusCommand command)
    {
        if (command.ExperienceId == Guid.Empty)
        {
            throw new ValidationException("ExperienceId is required.");
        }

        if (!Enum.IsDefined(command.ApprovalStatus))
            throw new ValidationException("ApprovalStatus is invalid.");

        if (!string.IsNullOrWhiteSpace(command.ModerationNotes) &&
            command.ModerationNotes.Length > 1000)
        {
            throw new ValidationException("ModerationNotes cannot exceed 1000 characters.");
        }
    }
}
