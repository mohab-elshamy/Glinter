using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceBookings;

public class GetExperienceBookingsQueryHandler
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceBookingRepository _bookingRepository;
    private readonly IExperienceProfileResolver _profileResolver;

    public GetExperienceBookingsQueryHandler(
        IExperienceRepository experienceRepository,
        IExperienceBookingRepository bookingRepository,
        IExperienceProfileResolver profileResolver)
    {
        _experienceRepository = experienceRepository;
        _bookingRepository = bookingRepository;
        _profileResolver = profileResolver;
    }

    public async Task<List<ExperienceBookingResponseDto>?> HandleAsync(
        GetExperienceBookingsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.ExperienceId == Guid.Empty)
        {
            throw new ArgumentException("ExperienceId is required.");
        }

        Glinter.Modules.Experiences.Application.Common.ExperiencePagination.Validate(
            query.Page,
            query.PageSize);

        var providerProfileId = await _profileResolver
            .GetCurrentExperienceProviderProfileIdAsync(cancellationToken);

        var experience = await _experienceRepository.GetByIdAsync(
            query.ExperienceId,
            cancellationToken);

        if (experience == null)
        {
            return null;
        }

        if (experience.ProviderProfileId != providerProfileId)
        {
            throw new UnauthorizedAccessException("You can view bookings only for your own experiences.");
        }

        var bookings = await _bookingRepository.GetByExperienceIdAsync(
            query.ExperienceId,
            query.Page,
            query.PageSize,
            cancellationToken);

        return bookings
            .Select(ExperiencesMappings.ToBookingResponse)
            .ToList();
    }
}
