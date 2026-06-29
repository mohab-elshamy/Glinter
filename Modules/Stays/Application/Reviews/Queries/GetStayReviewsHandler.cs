using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Reviews.Dtos;

namespace Glinter.Modules.Stays.Application.Reviews.Queries;

public class GetStayReviewsHandler
{
    private readonly IStayRepository _stayRepository;
    private readonly IStayReviewRepository _stayReviewRepository;

    public GetStayReviewsHandler(
        IStayRepository stayRepository,
        IStayReviewRepository stayReviewRepository)
    {
        _stayRepository = stayRepository;
        _stayReviewRepository = stayReviewRepository;
    }

    public async Task<List<StayReviewResponseDto>> HandleAsync(
        GetStayReviewsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.StayId == Guid.Empty)
            throw new ValidationException("Stay id is required.");

        var stay = await _stayRepository.GetByIdAsync(query.StayId, cancellationToken);

        if (stay is null || !stay.IsActive)
            throw new NotFoundException("Stay not found.");

        var reviews = await _stayReviewRepository.GetByStayIdAsync(query.StayId, cancellationToken);

        return reviews.Select(r => new StayReviewResponseDto
        {
            Id = r.Id,
            StayId = r.StayId,
            TravelerProfileId = r.TravelerProfileId,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAtUtc = r.CreatedAtUtc
        }).ToList();
    }
}
