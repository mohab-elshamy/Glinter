using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.SetExperienceApprovalStatus;

public class SetExperienceApprovalStatusCommandHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;
    private readonly SetExperienceApprovalStatusCommandValidator _validator;

    public SetExperienceApprovalStatusCommandHandler(
        IExperienceRepository experienceRepository,
        Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService regionReferenceService,
        SetExperienceApprovalStatusCommandValidator validator)
    {
        _experienceRepository = experienceRepository;
        _regionReferenceService = regionReferenceService;
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

        if (updatedExperience is null)
        {
            return null;
        }

        var region = await _regionReferenceService.GetNeighbourhoodAsync(
            updatedExperience.Adm3Gid,
            cancellationToken);

        return ExperiencesMappings.ToExperienceResponse(updatedExperience, region);
    }
}
