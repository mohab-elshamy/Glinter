using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Domain.Enums;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceAvailability;

public class GetExperienceAvailabilityQueryHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceAvailabilityRepository _availabilityRepository;

    public GetExperienceAvailabilityQueryHandler(
        IExperienceRepository experienceRepository,
        IExperienceAvailabilityRepository availabilityRepository)
    {
        _experienceRepository = experienceRepository;
        _availabilityRepository = availabilityRepository;
    }

    public async Task<List<ExperienceAvailabilityResponseDto>?> HandleAsync(
        GetExperienceAvailabilityQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.ExperienceId == Guid.Empty)
        {
            throw new ArgumentException("ExperienceId is required.");
        }

        Glinter.Modules.Experiences.Application.Common.ExperiencePagination.Validate(
            query.Page,
            query.PageSize);

        var experience = await _experienceRepository.GetByIdAsync(
            query.ExperienceId,
            cancellationToken);

        if (experience == null)
        {
            return null;
        }

        if (!experience.IsActive || experience.ApprovalStatus != ExperienceApprovalStatus.Approved)
        {
            return null;
        }

        var availability = await _availabilityRepository.GetByExperienceIdAsync(
            query.ExperienceId,
            query.Page,
            query.PageSize,
            cancellationToken);

        return availability
            .Select(ExperiencesMappings.ToAvailabilityResponse)
            .ToList();
    }
}
