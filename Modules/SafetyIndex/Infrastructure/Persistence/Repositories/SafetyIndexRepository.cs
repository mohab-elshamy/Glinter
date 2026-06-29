using Glinter.Modules.SafetyIndex.Application.Abstractions;
using Glinter.Modules.SafetyIndex.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.SafetyIndex.Infrastructure.Persistence.Repositories;

public class SafetyIndexRepository(SafetyIndexDbContext db) : ISafetyIndexRepository
{
    public Task<SafetyIndexResult?> GetByAdm2GidAsync(int adm2Gid, CancellationToken ct = default)
        => db.SafetyIndexResults.FirstOrDefaultAsync(x => x.Adm2Gid == adm2Gid, ct);

    public Task<List<SafetyIndexResult>> GetByAdm2GidsAsync(
        IReadOnlyCollection<int> adm2Gids,
        CancellationToken ct = default)
        => db.SafetyIndexResults
            .Where(x => adm2Gids.Contains(x.Adm2Gid))
            .ToListAsync(ct);

    public async Task UpsertAsync(SafetyIndexResult result, CancellationToken ct = default)
    {
        result.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (result.Id == 0)
        {
            result.CreatedAtUtc = DateTimeOffset.UtcNow;
            db.SafetyIndexResults.Add(result);
        }
        else
        {
            db.SafetyIndexResults.Update(result);
        }

        await db.SaveChangesAsync(ct);
    }
}
