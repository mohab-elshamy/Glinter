using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Repositories;

public class ExperienceReviewRepository : IExperienceReviewRepository
{
    private readonly ExperiencesDbContext _context;

    public ExperienceReviewRepository(ExperiencesDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ExperienceReview review, CancellationToken cancellationToken = default)
    {
        await _context.ExperienceReviews.AddAsync(review, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ExperienceReview>> GetByExperienceIdAsync(
        Guid experienceId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceReviews
            .AsNoTracking()
            .Where(x => x.ExperienceId == experienceId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExperienceReview?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceReviews
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<ExperienceReview?> GetForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceReviews
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid experienceId,
        Guid travelerProfileId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceReviews.AnyAsync(
            x => x.ExperienceId == experienceId &&
                 x.TravelerProfileId == travelerProfileId,
            cancellationToken);
    }

    public async Task UpdateAsync(ExperienceReview review, CancellationToken cancellationToken = default)
    {
        _context.ExperienceReviews.Update(review);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ExperienceReview review, CancellationToken cancellationToken = default)
    {
        _context.ExperienceReviews.Remove(review);
        await _context.SaveChangesAsync(cancellationToken);
    }
}