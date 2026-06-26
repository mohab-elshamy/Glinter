using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Domain.Entities;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Experiences.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Repositories;

public class ExperienceBookingRepository : IExperienceBookingRepository
{
    private const decimal MaxDatabaseMoneyValue = 9_999_999_999_999_999.99m;

    private readonly ExperiencesDbContext _context;

    public ExperienceBookingRepository(ExperiencesDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ExperienceBooking booking, CancellationToken cancellationToken = default)
    {
        await _context.ExperienceBookings.AddAsync(booking, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ExperienceBooking>> GetByExperienceIdAsync(
        Guid experienceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagination = ExperiencePagination.Normalize(page, pageSize);

        return await _context.ExperienceBookings
            .AsNoTracking()
            .Where(x => x.ExperienceId == experienceId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExperienceBooking?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceBookings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<ExperienceBooking?> GetForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceBookings
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> HasActiveBookingAsync(
        Guid availabilityId,
        Guid travelerProfileId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceBookings.AnyAsync(
            x => x.AvailabilityId == availabilityId &&
                 x.TravelerProfileId == travelerProfileId &&
                 x.Status != ExperienceBookingStatus.Cancelled,
            cancellationToken);
    }

    public async Task<bool> HasCompletedBookingAsync(
        Guid experienceId,
        Guid travelerProfileId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceBookings.AnyAsync(
            x => x.ExperienceId == experienceId &&
                 x.TravelerProfileId == travelerProfileId &&
                 x.Status == ExperienceBookingStatus.Completed,
            cancellationToken);
    }

    public async Task UpdateAsync(
        ExperienceBooking booking,
        CancellationToken cancellationToken = default)
    {
        var completedAtUtc = booking.CompletedAtUtc;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM experience_bookings WHERE "Id" = {booking.Id} FOR UPDATE""",
            cancellationToken);

        await _context.Entry(booking).ReloadAsync(cancellationToken);

        if (booking.Status == ExperienceBookingStatus.Cancelled)
            throw new InvalidOperationException("Cancelled booking cannot be completed.");

        if (booking.Status == ExperienceBookingStatus.Completed)
            throw new InvalidOperationException("Booking is already completed.");

        booking.Status = ExperienceBookingStatus.Completed;
        booking.CompletedAtUtc = completedAtUtc ?? DateTime.UtcNow;

        _context.ExperienceBookings.Update(booking);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AddBookingAndUpdateAvailabilityAsync(
        ExperienceBooking booking,
        ExperienceAvailability availability,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM experiences WHERE "Id" = {booking.ExperienceId} FOR UPDATE""",
            cancellationToken);

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM experience_availability WHERE "Id" = {booking.AvailabilityId} FOR UPDATE""",
            cancellationToken);

        var currentExperience = await _context.Experiences
            .AsNoTracking()
            .Where(x => x.Id == booking.ExperienceId)
            .Select(x => new
            {
                x.IsActive,
                x.ApprovalStatus,
                x.MaxGuests,
                x.PricePerPerson
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (currentExperience is null)
            throw new KeyNotFoundException("Experience was not found.");

        if (!currentExperience.IsActive ||
            currentExperience.ApprovalStatus != ExperienceApprovalStatus.Approved)
            throw new InvalidOperationException("Cannot book an experience that is not active and approved.");

        if (booking.GuestsCount > currentExperience.MaxGuests)
            throw new InvalidOperationException(
                "GuestsCount exceeds the maximum guests allowed for this experience.");

        await _context.Entry(availability).ReloadAsync(cancellationToken);

        if (availability.ExperienceId != booking.ExperienceId)
            throw new InvalidOperationException("Availability slot does not belong to this experience.");

        if (!availability.IsActive)
            throw new InvalidOperationException("Availability slot is not active.");

        if (availability.StartTimeUtc <= DateTime.UtcNow)
            throw new InvalidOperationException("Cannot book an availability slot in the past.");

        if (booking.GuestsCount > availability.Capacity - availability.BookedCount)
            throw new InvalidOperationException("Not enough remaining capacity for this availability slot.");

        var alreadyBooked = await _context.ExperienceBookings.AnyAsync(
            x => x.AvailabilityId == booking.AvailabilityId &&
                 x.TravelerProfileId == booking.TravelerProfileId &&
                 x.Status != ExperienceBookingStatus.Cancelled,
            cancellationToken);

        if (alreadyBooked)
            throw new InvalidOperationException(
                "You already have an active booking for this availability slot.");

        var totalPrice = currentExperience.PricePerPerson * booking.GuestsCount;
        if (totalPrice > MaxDatabaseMoneyValue)
            throw new ArgumentException("The booking total exceeds the maximum supported value.");

        booking.TotalPrice = totalPrice;
        availability.BookedCount += booking.GuestsCount;

        _context.ExperienceAvailability.Update(availability);
        await _context.ExperienceBookings.AddAsync(booking, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateBookingAndAvailabilityAsync(
        ExperienceBooking booking,
        ExperienceAvailability availability,
        CancellationToken cancellationToken = default)
    {
        var cancelledAtUtc = booking.CancelledAtUtc;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM experience_bookings WHERE "Id" = {booking.Id} FOR UPDATE""",
            cancellationToken);

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM experience_availability WHERE "Id" = {availability.Id} FOR UPDATE""",
            cancellationToken);

        await _context.Entry(booking).ReloadAsync(cancellationToken);
        await _context.Entry(availability).ReloadAsync(cancellationToken);

        if (booking.Status == ExperienceBookingStatus.Cancelled)
            throw new InvalidOperationException("Booking is already cancelled.");

        if (booking.Status == ExperienceBookingStatus.Completed)
            throw new InvalidOperationException("Completed booking cannot be cancelled.");

        if (availability.StartTimeUtc <= DateTime.UtcNow)
            throw new InvalidOperationException("A booking cannot be cancelled after the experience has started.");

        booking.Status = ExperienceBookingStatus.Cancelled;
        booking.CancelledAtUtc = cancelledAtUtc ?? DateTime.UtcNow;
        availability.BookedCount = Math.Max(0, availability.BookedCount - booking.GuestsCount);

        _context.ExperienceBookings.Update(booking);
        _context.ExperienceAvailability.Update(availability);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<List<ExperienceBooking>> GetByTravelerProfileIdAsync(
        Guid travelerProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagination = ExperiencePagination.Normalize(page, pageSize);

        return await _context.ExperienceBookings
            .AsNoTracking()
            .Where(x => x.TravelerProfileId == travelerProfileId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);
    }
}
