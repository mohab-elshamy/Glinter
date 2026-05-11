using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Domain.Entities;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Repositories;

public class StayReviewRepository : IStayReviewRepository
{
    private readonly StaysDbContext _dbContext;

    public StayReviewRepository(StaysDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StayReview> AddAsync(StayReview review, CancellationToken cancellationToken = default)
    {
        _dbContext.StayReviews.Add(review);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return review;
    }

    public async Task<List<StayReview>> GetByStayIdAsync(Guid stayId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StayReviews
            .Where(x => x.StayId == stayId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid stayId, Guid travelerProfileId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StayReviews.AnyAsync(
            x => x.StayId == stayId
                 && x.TravelerProfileId == travelerProfileId,
            cancellationToken);
    }

    public async Task<StayReview?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StayReviews
            .FirstOrDefaultAsync(x => x.Id == reviewId, cancellationToken);
    }

    public async Task<StayReview> UpdateAsync(StayReview review, CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
        return review;
    }
    public async Task DeleteAsync(StayReview review, CancellationToken cancellationToken = default)
    {
        _dbContext.StayReviews.Remove(review);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}