using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Domain.Entities;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Repositories;

public class StayBookingRepository : IStayBookingRepository
{
    private const decimal MaxDatabaseMoneyValue = 9_999_999_999_999_999.99m;

    private readonly StaysDbContext _dbContext;

    public StayBookingRepository(StaysDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StayBooking> AddAsync(StayBooking booking, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM stays WHERE "Id" = {booking.StayId} FOR UPDATE""",
            cancellationToken);

        var currentStay = await _dbContext.Stays
            .AsNoTracking()
            .Where(x => x.Id == booking.StayId)
            .Select(x => new
            {
                x.IsActive,
                x.MaxGuests,
                x.PricePerNight
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (currentStay is null)
            throw new NotFoundException("Stay not found.");

        if (!currentStay.IsActive)
            throw new ConflictException("This stay is not active.");

        if (booking.GuestCount > currentStay.MaxGuests)
            throw new ValidationException("GuestCount exceeds the maximum allowed guests for this stay.");

        var hasOverlap = await _dbContext.StayBookings.AnyAsync(
            x => x.StayId == booking.StayId
                 && x.Status != "Cancelled"
                 && x.CheckInDate < booking.CheckOutDate
                 && booking.CheckInDate < x.CheckOutDate,
            cancellationToken);

        if (hasOverlap)
            throw new ConflictException("This stay is already booked for the selected dates.");

        var nights = booking.CheckOutDate.DayNumber - booking.CheckInDate.DayNumber;
        var totalPrice = nights * currentStay.PricePerNight;

        if (totalPrice > MaxDatabaseMoneyValue)
            throw new ValidationException("The booking total exceeds the maximum supported value.");

        booking.TotalPrice = totalPrice;

        _dbContext.StayBookings.Add(booking);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

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
                 && x.Status != "Cancelled"
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
                 && x.Status != "Cancelled"
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
