using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetMyExperienceBookings;

public class GetMyExperienceBookingsQueryHandler
{
    private readonly IExperienceBookingRepository _bookingRepository;
    private readonly IExperienceProfileResolver _profileResolver;

    public GetMyExperienceBookingsQueryHandler(
        IExperienceBookingRepository bookingRepository,
        IExperienceProfileResolver profileResolver)
    {
        _bookingRepository = bookingRepository;
        _profileResolver = profileResolver;
    }

    public async Task<List<ExperienceBookingResponseDto>> HandleAsync(
        GetMyExperienceBookingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var travelerProfileId = await _profileResolver
            .GetCurrentTravelerProfileIdAsync(cancellationToken);

        var bookings = await _bookingRepository.GetByTravelerProfileIdAsync(
            travelerProfileId,
            cancellationToken);

        return bookings
            .Select(ExperiencesMappings.ToBookingResponse)
            .ToList();
    }
}