using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Domain.Entities;
using Glinter.Modules.Experiences.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Repositories;

public class ExperienceBookingRepository : IExperienceBookingRepository
{
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
        CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceBookings
            .AsNoTracking()
            .Where(x => x.ExperienceId == experienceId)
            .OrderByDescending(x => x.CreatedAtUtc)
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
        _context.ExperienceBookings.Update(booking);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddBookingAndUpdateAvailabilityAsync(
        ExperienceBooking booking,
        ExperienceAvailability availability,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _context.ExperienceAvailability.Update(availability);
            await _context.ExperienceBookings.AddAsync(booking, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateBookingAndAvailabilityAsync(
        ExperienceBooking booking,
        ExperienceAvailability availability,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _context.ExperienceBookings.Update(booking);
            _context.ExperienceAvailability.Update(availability);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
    
    public async Task<List<ExperienceBooking>> GetByTravelerProfileIdAsync(
        Guid travelerProfileId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceBookings
            .AsNoTracking()
            .Where(x => x.TravelerProfileId == travelerProfileId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}