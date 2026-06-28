using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Profiles.Queries;

public sealed class GetLocalBuddyVerificationHistoryHandler
{
    private readonly IProfilesDbContext _dbContext;

    public GetLocalBuddyVerificationHistoryHandler(IProfilesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<LocalBuddyVerificationEventDto>> HandleAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ValidationException("UserId is required.");
        if (page is < 1 or > 10000)
            throw new ValidationException("Page must be between 1 and 10000.");
        if (pageSize is < 1 or > 100)
            throw new ValidationException("PageSize must be between 1 and 100.");
        if (!await _dbContext.LocalBuddyProfiles.AnyAsync(
                x => x.UserId == userId,
                cancellationToken))
        {
            throw new NotFoundException("Local buddy profile not found.");
        }

        return await _dbContext.LocalBuddyVerificationEvents
            .AsNoTracking()
            .Where(x => x.LocalBuddyUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LocalBuddyVerificationEventDto
            {
                Id = x.Id,
                LocalBuddyUserId = x.LocalBuddyUserId,
                ActorUserId = x.ActorUserId,
                PreviousStatus = x.PreviousStatus.ToString(),
                NewStatus = x.NewStatus.ToString(),
                Notes = x.Notes,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
