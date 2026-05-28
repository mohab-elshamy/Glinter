using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Domain.Entities;
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
        await _context.ExperienceAvailability.AddAsync(availability, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ExperienceAvailability>> GetByExperienceIdAsync(
        Guid experienceId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceAvailability
            .AsNoTracking()
            .Where(x => x.ExperienceId == experienceId)
            .OrderBy(x => x.StartTimeUtc)
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
        _context.ExperienceAvailability.Update(availability);
        await _context.SaveChangesAsync(cancellationToken);
    }
}