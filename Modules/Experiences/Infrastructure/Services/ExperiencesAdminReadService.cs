using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Infrastructure.Services;

public sealed class ExperiencesAdminReadService(
    ExperiencesDbContext dbContext) : IExperiencesAdminReadService
{
    public async Task<ExperiencesAdminSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        var statuses = await dbContext.ExperienceBookings.AsNoTracking()
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);
        return new(
            await dbContext.Experiences.CountAsync(
                x => x.ModerationStatus == ExperienceModerationStatus.Pending,
                cancellationToken),
            await dbContext.Experiences.CountAsync(
                x => x.ModerationStatus == ExperienceModerationStatus.Approved,
                cancellationToken),
            await dbContext.Experiences.CountAsync(cancellationToken),
            await dbContext.ExperienceBookings.CountAsync(cancellationToken),
            await dbContext.ExperienceBookings.SumAsync(
                x => (decimal?)x.TotalPrice,
                cancellationToken) ?? 0,
            statuses.Select(status => status.ToString()).ToList());
    }
}
