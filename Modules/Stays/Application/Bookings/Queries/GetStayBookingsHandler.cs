using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Bookings.Dtos;

namespace Glinter.Modules.Stays.Application.Bookings.Queries;

public class GetStayBookingsHandler
{
    private readonly IStayRepository _stayRepository;
    private readonly IStayBookingRepository _stayBookingRepository;

    public GetStayBookingsHandler(
        IStayRepository stayRepository,
        IStayBookingRepository stayBookingRepository)
    {
        _stayRepository = stayRepository;
        _stayBookingRepository = stayBookingRepository;
    }

    public async Task<List<StayBookingResponseDto>> HandleAsync(
        GetStayBookingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var stay = await _stayRepository.GetByIdAsync(query.StayId, cancellationToken);

        if (stay is null)
            throw new KeyNotFoundException("Stay not found.");

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
}