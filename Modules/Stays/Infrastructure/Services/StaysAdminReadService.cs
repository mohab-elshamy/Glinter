using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Stays.Infrastructure.Services;

public sealed class StaysAdminReadService(
    StaysDbContext dbContext) : IStaysAdminReadService
{
    public async Task<StaysAdminSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        var statuses = await dbContext.StayBookings.AsNoTracking()
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);
        return new(
            await dbContext.Stays.CountAsync(x => x.IsActive, cancellationToken),
            await dbContext.Stays.CountAsync(cancellationToken),
            await dbContext.StayBookings.CountAsync(cancellationToken),
            await dbContext.StayBookings.SumAsync(
                x => (decimal?)x.TotalPrice,
                cancellationToken) ?? 0,
            statuses.Select(status => status.ToString()).ToList());
    }
}
