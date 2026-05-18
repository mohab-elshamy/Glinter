using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Repositories;

public class ExperienceRepository : IExperienceRepository
{
    private readonly ExperiencesDbContext _context;

    public ExperienceRepository(ExperiencesDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Experience experience, CancellationToken cancellationToken = default)
    {
        await _context.Experiences.AddAsync(experience, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Experience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Experiences
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Tags)
            .Include(x => x.ExperienceVibes)
                .ThenInclude(x => x.Vibe)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Experience?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Experiences
            .Include(x => x.Tags)
            .Include(x => x.ExperienceVibes)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<Experience>> GetFilteredAsync(
        Guid? areaId,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        int? guests,
        Guid? vibeId,
        string? tag,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Experiences
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Tags)
            .Include(x => x.ExperienceVibes)
                .ThenInclude(x => x.Vibe)
            .Where(x => x.IsActive)
            .AsQueryable();

        if (areaId.HasValue)
        {
            query = query.Where(x => x.AreaId == areaId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(x => x.PricePerPerson >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(x => x.PricePerPerson <= maxPrice.Value);
        }

        if (guests.HasValue)
        {
            query = query.Where(x => x.MaxGuests >= guests.Value);
        }

        if (vibeId.HasValue)
        {
            query = query.Where(x => x.ExperienceVibes.Any(v => v.VibeId == vibeId.Value));
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var normalizedTag = tag.Trim().ToLower();
            query = query.Where(x => x.Tags.Any(t => t.Name.ToLower() == normalizedTag));
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid providerProfileId,
        string title,
        Guid areaId,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim().ToLower();

        return await _context.Experiences.AnyAsync(
            x => x.ProviderProfileId == providerProfileId &&
                 x.AreaId == areaId &&
                 x.Title.ToLower() == normalizedTitle,
            cancellationToken);
    }

    public async Task ReplaceTagsAsync(
        Guid experienceId,
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default)
    {
        var existingTags = await _context.ExperienceTags
            .Where(x => x.ExperienceId == experienceId)
            .ToListAsync(cancellationToken);

        _context.ExperienceTags.RemoveRange(existingTags);

        var newTags = tags
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => new ExperienceTag
            {
                Id = Guid.NewGuid(),
                ExperienceId = experienceId,
                Name = x
            })
            .ToList();

        await _context.ExperienceTags.AddRangeAsync(newTags, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceVibesAsync(
        Guid experienceId,
        IEnumerable<Guid> vibeIds,
        CancellationToken cancellationToken = default)
    {
        var existingVibes = await _context.ExperienceVibes
            .Where(x => x.ExperienceId == experienceId)
            .ToListAsync(cancellationToken);

        _context.ExperienceVibes.RemoveRange(existingVibes);

        var newVibes = vibeIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Select(x => new ExperienceVibe
            {
                ExperienceId = experienceId,
                VibeId = x
            })
            .ToList();

        await _context.ExperienceVibes.AddRangeAsync(newVibes, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Experience experience, CancellationToken cancellationToken = default)
    {
        _context.Experiences.Update(experience);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<List<Experience>> GetByProviderProfileIdAsync(
        Guid providerProfileId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Experiences
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Tags)
            .Include(x => x.ExperienceVibes)
            .ThenInclude(x => x.Vibe)
            .Where(x => x.ProviderProfileId == providerProfileId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}