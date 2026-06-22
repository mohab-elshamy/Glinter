using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Domain.Entities;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Repositories;

public class StayBookingRepository : IStayBookingRepository
{
    private readonly StaysDbContext _dbContext;

    public StayBookingRepository(StaysDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StayBooking> AddAsync(StayBooking booking, CancellationToken cancellationToken = default)
    {
        _dbContext.StayBookings.Add(booking);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return booking;
    }

    public async Task<List<StayBooking>> GetByStayIdAsync(Guid stayId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StayBookings
            .Where(x => x.StayId == stayId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
    public async Task<bool> ExistsAsync(
        Guid stayId,
        Guid travelerProfileId,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StayBookings.AnyAsync(
            x => x.StayId == stayId
                 && x.TravelerProfileId == travelerProfileId
                 && x.CheckInDate == checkInDate
                 && x.CheckOutDate == checkOutDate,
            cancellationToken);
    }
    public async Task<bool> HasOverlapAsync(
        Guid stayId,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StayBookings.AnyAsync(
            x => x.StayId == stayId
                 && x.CheckInDate < checkOutDate
                 && checkInDate < x.CheckOutDate,
            cancellationToken);
    }

    public async Task<StayBooking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StayBookings
            .FirstOrDefaultAsync(x => x.Id == bookingId, cancellationToken);
    }

    public async Task<StayBooking> UpdateAsync(StayBooking booking, CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
        return booking;
    }

    public async Task<bool> HasEligibleReviewBookingAsync(
        Guid stayId,
        Guid travelerProfileId,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StayBookings.AnyAsync(
            x => x.StayId == stayId
                 && x.TravelerProfileId == travelerProfileId
                 && x.Status != "Cancelled"
                 && x.CheckOutDate <= today,
            cancellationToken);
    }
}