using Glinter.Modules.Stays.Domain.Entities;

namespace Glinter.Modules.Stays.Application.Abstractions;

public interface IStayBookingRepository
{
    Task<StayBooking> AddAsync(StayBooking booking, CancellationToken cancellationToken = default);
    Task<List<StayBooking>> GetByStayIdAsync(Guid stayId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid stayId,
        Guid travelerProfileId,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlapAsync(
        Guid stayId,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        CancellationToken cancellationToken = default);

    Task<StayBooking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<StayBooking> UpdateAsync(StayBooking booking, CancellationToken cancellationToken = default);

    Task<bool> HasEligibleReviewBookingAsync(
        Guid stayId,
        Guid travelerProfileId,
        DateOnly today,
        CancellationToken cancellationToken = default);
}