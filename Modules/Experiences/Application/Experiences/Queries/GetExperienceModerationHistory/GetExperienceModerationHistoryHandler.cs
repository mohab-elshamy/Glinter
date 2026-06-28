using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceModerationHistory;

public sealed class GetExperienceModerationHistoryHandler
{
    private readonly IExperiencesDbContext _dbContext;

    public GetExperienceModerationHistoryHandler(IExperiencesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ExperienceModerationEventDto>> HandleAsync(
        Guid experienceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (experienceId == Guid.Empty)
            throw new ValidationException("ExperienceId is required.");
        if (page is < 1 or > 10000)
            throw new ValidationException("Page must be between 1 and 10000.");
        if (pageSize is < 1 or > 100)
            throw new ValidationException("PageSize must be between 1 and 100.");
        if (!await _dbContext.Experiences.AnyAsync(
                x => x.Id == experienceId,
                cancellationToken))
        {
            throw new NotFoundException("Experience was not found.");
        }

        return await _dbContext.ExperienceModerationEvents
            .AsNoTracking()
            .Where(x => x.ExperienceId == experienceId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ExperienceModerationEventDto
            {
                Id = x.Id,
                ExperienceId = x.ExperienceId,
                ActorUserId = x.ActorUserId,
                Action = x.Action,
                PreviousStatus = x.PreviousStatus.ToString(),
                NewStatus = x.NewStatus.ToString(),
                Notes = x.Notes,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
