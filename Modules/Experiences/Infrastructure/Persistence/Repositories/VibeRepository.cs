using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Repositories;

public class VibeRepository : IVibeRepository
{
    private readonly ExperiencesDbContext _context;

    public VibeRepository(ExperiencesDbContext context)
    {
        _context = context;
    }

    public async Task<List<Vibe>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Vibes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Vibe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Vibes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAllAsync(
        IEnumerable<Guid> vibeIds,
        CancellationToken cancellationToken = default)
    {
        var ids = vibeIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return true;
        }

        var count = await _context.Vibes.CountAsync(
            x => ids.Contains(x.Id) && x.IsActive,
            cancellationToken);

        return count == ids.Count;
    }
}