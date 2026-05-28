using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.SetExperienceApprovalStatus;

public class SetExperienceApprovalStatusCommandHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly SetExperienceApprovalStatusCommandValidator _validator;

    public SetExperienceApprovalStatusCommandHandler(
        IExperienceRepository experienceRepository,
        SetExperienceApprovalStatusCommandValidator validator)
    {
        _experienceRepository = experienceRepository;
        _validator = validator;
    }

    public async Task<ExperienceResponseDto?> HandleAsync(
        SetExperienceApprovalStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        _validator.Validate(command);

        var experience = await _experienceRepository.GetForUpdateAsync(
            command.ExperienceId,
            cancellationToken);

        if (experience == null)
        {
            return null;
        }

        experience.ApprovalStatus = command.ApprovalStatus;
        experience.ModerationNotes = string.IsNullOrWhiteSpace(command.ModerationNotes)
            ? null
            : command.ModerationNotes.Trim();
        experience.ModeratedAtUtc = DateTime.UtcNow;
        experience.UpdatedAtUtc = DateTime.UtcNow;

        await _experienceRepository.UpdateAsync(experience, cancellationToken);

        var updatedExperience = await _experienceRepository.GetByIdAsync(
            experience.Id,
            cancellationToken);

        return updatedExperience == null
            ? null
            : ExperiencesMappings.ToExperienceResponse(updatedExperience);
    }
}