using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Domain.Entities;
using Glinter.Modules.Experiences.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Repositories;

public class ExperienceAvailabilityRepository : IExperienceAvailabilityRepository
{
    private readonly ExperiencesDbContext _context;

    public ExperienceAvailabilityRepository(ExperiencesDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ExperienceAvailability availability, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM experiences WHERE "Id" = {availability.ExperienceId} FOR UPDATE""",
            cancellationToken);

        var experienceIsActive = await _context.Experiences
            .AsNoTracking()
            .Where(x => x.Id == availability.ExperienceId)
            .Select(x => (bool?)x.IsActive)
            .SingleOrDefaultAsync(cancellationToken);

        if (experienceIsActive is null)
            throw new NotFoundException("Experience was not found.");

        if (!experienceIsActive.Value)
            throw new ConflictException("Cannot add availability to an inactive experience.");

        var hasOverlap = await _context.ExperienceAvailability.AnyAsync(
            x => x.ExperienceId == availability.ExperienceId &&
                 x.IsActive &&
                 availability.StartTimeUtc < x.EndTimeUtc &&
                 availability.EndTimeUtc > x.StartTimeUtc,
            cancellationToken);

        if (hasOverlap)
            throw new ConflictException("This availability slot overlaps with an existing active slot.");

        await _context.ExperienceAvailability.AddAsync(availability, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<List<ExperienceAvailability>> GetByExperienceIdAsync(
        Guid experienceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagination = ExperiencePagination.Normalize(page, pageSize);
        var nowUtc = DateTime.UtcNow;

        return await _context.ExperienceAvailability
            .AsNoTracking()
            .Where(x =>
                x.ExperienceId == experienceId &&
                x.IsActive &&
                x.StartTimeUtc > nowUtc)
            .OrderBy(x => x.StartTimeUtc)
            .ThenBy(x => x.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExperienceAvailability?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceAvailability
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<ExperienceAvailability?> GetForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceAvailability
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> HasOverlapAsync(
        Guid experienceId,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceAvailability.AnyAsync(
            x => x.ExperienceId == experienceId &&
                 x.IsActive &&
                 startTimeUtc < x.EndTimeUtc &&
                 endTimeUtc > x.StartTimeUtc,
            cancellationToken);
    }

    public async Task UpdateAsync(
        ExperienceAvailability availability,
        CancellationToken cancellationToken = default)
    {
        var targetIsActive = availability.IsActive;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM experiences WHERE "Id" = {availability.ExperienceId} FOR UPDATE""",
            cancellationToken);

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM experience_availability WHERE "Id" = {availability.Id} FOR UPDATE""",
            cancellationToken);

        await _context.Entry(availability).ReloadAsync(cancellationToken);

        if (targetIsActive)
        {
            var experienceIsActive = await _context.Experiences
                .AsNoTracking()
                .Where(x => x.Id == availability.ExperienceId)
                .Select(x => (bool?)x.IsActive)
                .SingleOrDefaultAsync(cancellationToken);

            if (experienceIsActive != true)
                throw new ConflictException("Cannot activate availability for an inactive experience.");

            if (availability.StartTimeUtc <= DateTime.UtcNow)
                throw new ConflictException("Cannot activate an availability slot in the past.");

            var hasOverlap = await _context.ExperienceAvailability.AnyAsync(
                x => x.ExperienceId == availability.ExperienceId &&
                     x.Id != availability.Id &&
                     x.IsActive &&
                     availability.StartTimeUtc < x.EndTimeUtc &&
                     availability.EndTimeUtc > x.StartTimeUtc,
                cancellationToken);

            if (hasOverlap)
                throw new ConflictException(
                    "Cannot activate this slot because it overlaps with another active slot.");
        }

        availability.IsActive = targetIsActive;
        _context.ExperienceAvailability.Update(availability);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
