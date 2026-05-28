using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.SetExperienceActiveStatus;

public class SetExperienceActiveStatusCommandHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceProfileResolver _profileResolver;
    private readonly SetExperienceActiveStatusCommandValidator _validator;

    public SetExperienceActiveStatusCommandHandler(
        IExperienceRepository experienceRepository,
        IExperienceProfileResolver profileResolver,
        SetExperienceActiveStatusCommandValidator validator)
    {
        _experienceRepository = experienceRepository;
        _profileResolver = profileResolver;
        _validator = validator;
    }

    public async Task<ExperienceResponseDto?> HandleAsync(
        SetExperienceActiveStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        _validator.Validate(command);

        var providerProfileId = await _profileResolver
            .GetCurrentExperienceProviderProfileIdAsync(cancellationToken);

        var experience = await _experienceRepository.GetForUpdateAsync(
            command.ExperienceId,
            cancellationToken);

        if (experience == null)
        {
            return null;
        }

        if (experience.ProviderProfileId != providerProfileId)
        {
            throw new UnauthorizedAccessException("You can change status only for your own experiences.");
        }

        experience.IsActive = command.IsActive;
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