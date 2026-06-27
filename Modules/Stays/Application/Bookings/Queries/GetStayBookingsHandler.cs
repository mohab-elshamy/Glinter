using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Bookings.Dtos;

namespace Glinter.Modules.Stays.Application.Bookings.Queries;

public class GetStayBookingsHandler
{
    private readonly IStayRepository _stayRepository;
    private readonly IStayBookingRepository _stayBookingRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProfilesReadService _profilesReadService;

    public GetStayBookingsHandler(
        IStayRepository stayRepository,
        IStayBookingRepository stayBookingRepository,
        ICurrentUserService currentUserService,
        IProfilesReadService profilesReadService)
    {
        _stayRepository = stayRepository;
        _stayBookingRepository = stayBookingRepository;
        _currentUserService = currentUserService;
        _profilesReadService = profilesReadService;
    }

    public async Task<List<StayBookingResponseDto>> HandleAsync(
        GetStayBookingsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.StayId == Guid.Empty)
            throw new ValidationException("Stay id is required.");

        var ownerProfileId = await GetCurrentOwnerProfileIdAsync(cancellationToken);

        var stay = await _stayRepository.GetByIdAsync(query.StayId, cancellationToken);

        if (stay is null)
            throw new NotFoundException("Stay not found.");

        if (stay.OwnerProfileId != ownerProfileId)
            throw new ForbiddenException("You can view bookings only for your own stays.");

        var bookings = await _stayBookingRepository.GetByStayIdAsync(query.StayId, cancellationToken);

        return bookings.Select(b => new StayBookingResponseDto
        {
            Id = b.Id,
            StayId = b.StayId,
            TravelerProfileId = b.TravelerProfileId,
            CheckInDate = b.CheckInDate,
            CheckOutDate = b.CheckOutDate,
            GuestCount = b.GuestCount,
            TotalPrice = b.TotalPrice,
            Status = b.Status,
            CreatedAtUtc = b.CreatedAtUtc
        }).ToList();
    }

    private async Task<Guid> GetCurrentOwnerProfileIdAsync(CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");

        var ownerProfileId = await _profilesReadService.GetHotelOwnerProfileIdByUserIdAsync(
            _currentUserService.UserId.Value,
            cancellationToken);

        return ownerProfileId
               ?? throw new ForbiddenException("Only hotel owners can view stay bookings.");
    }
}
