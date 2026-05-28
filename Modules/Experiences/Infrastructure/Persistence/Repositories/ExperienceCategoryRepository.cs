using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Repositories;

public class ExperienceCategoryRepository : IExperienceCategoryRepository
{
    private readonly ExperiencesDbContext _context;

    public ExperienceCategoryRepository(ExperiencesDbContext context)
    {
        _context = context;
    }

    public async Task<List<ExperienceCategory>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceCategories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExperienceCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ExperienceCategories.AnyAsync(
            x => x.Id == id && x.IsActive,
            cancellationToken);
    }
}