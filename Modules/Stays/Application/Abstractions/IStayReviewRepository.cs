using Glinter.Modules.Stays.Domain.Entities;

namespace Glinter.Modules.Stays.Application.Abstractions;

public interface IStayReviewRepository
{
    Task<StayReview> AddAsync(StayReview review, CancellationToken cancellationToken = default);
    Task<List<StayReview>> GetByStayIdAsync(Guid stayId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid stayId, Guid travelerProfileId, CancellationToken cancellationToken = default);

    Task<StayReview?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken = default);
    Task<StayReview> UpdateAsync(StayReview review, CancellationToken cancellationToken = default);
    Task DeleteAsync(StayReview review, CancellationToken cancellationToken = default);
}