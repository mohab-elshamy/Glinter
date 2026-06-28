using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Domain.Entities;
using Glinter.Modules.IdentityAccess.Application.Abstractions;

namespace Glinter.Modules.Experiences.Application.Experiences.Commands.SetExperienceApprovalStatus;

public class SetExperienceApprovalStatusCommandHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService _regionReferenceService;
    private readonly SetExperienceApprovalStatusCommandValidator _validator;
    private readonly IExperiencesDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public SetExperienceApprovalStatusCommandHandler(
        IExperienceRepository experienceRepository,
        Glinter.Modules.Regions.Application.Abstractions.IRegionReferenceService regionReferenceService,
        SetExperienceApprovalStatusCommandValidator validator,
        IExperiencesDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _experienceRepository = experienceRepository;
        _regionReferenceService = regionReferenceService;
        _validator = validator;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
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

        var previousStatus = experience.ApprovalStatus;
        experience.ApprovalStatus = command.ApprovalStatus;
        experience.ModerationNotes = string.IsNullOrWhiteSpace(command.ModerationNotes)
            ? null
            : command.ModerationNotes.Trim();
        experience.ModeratedAtUtc = DateTime.UtcNow;
        experience.UpdatedAtUtc = DateTime.UtcNow;
        _dbContext.ExperienceModerationEvents.Add(new ExperienceModerationEvent
        {
            Id = Guid.NewGuid(),
            ExperienceId = experience.Id,
            ActorUserId = GetCurrentUserId(),
            Action = "AdminDecision",
            PreviousStatus = previousStatus,
            NewStatus = command.ApprovalStatus,
            Notes = experience.ModerationNotes,
            CreatedAtUtc = DateTime.UtcNow
        });

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

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");
        return _currentUserService.UserId.Value;
    }
}
