using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Domain.Entities;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Repositories;

public class StayRepository : IStayRepository
{
    private readonly StaysDbContext _dbContext;

    public StayRepository(StaysDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Stay> AddAsync(Stay stay, CancellationToken cancellationToken = default)
    {
        _dbContext.Stays.Add(stay);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return stay;
    }

    public async Task<List<Stay>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Stays
            .Include(x => x.Tags)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Stay?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Stays
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Stay?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Stays
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid ownerProfileId,
        string name,
        string address,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        var normalizedAddress = address.Trim().ToLower();

        return await _dbContext.Stays.AnyAsync(
            x => x.OwnerProfileId == ownerProfileId
                 && x.Name.ToLower() == normalizedName
                 && x.Address.ToLower() == normalizedAddress,
            cancellationToken);
    }

    public async Task ReplaceTagsAsync(
        Guid stayId,
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.StayTags
            .Where(x => x.StayId == stayId)
            .ExecuteDeleteAsync(cancellationToken);

        var newTags = tags
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => new StayTag
            {
                Id = Guid.NewGuid(),
                StayId = stayId,
                Name = x.Trim()
            })
            .ToList();

        if (newTags.Count > 0)
        {
            await _dbContext.StayTags.AddRangeAsync(newTags, cancellationToken);
        }
    }

    public async Task<Stay> UpdateAsync(Stay stay, CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
        return stay;
    }

    public async Task<List<Stay>> GetByAdm3GidAsync(int adm3Gid, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Stays
            .Include(x => x.Tags)
            .Where(x => x.Adm3Gid == adm3Gid && x.IsActive)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Stay>> GetFilteredAsync(
        int? adm3Gid,
        decimal? minPrice,
        decimal? maxPrice,
        int? guests,
        string? tag,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Stays
            .Include(x => x.Tags)
            .Where(x => x.IsActive)
            .AsQueryable();

        if (adm3Gid.HasValue)
        {
            query = query.Where(x => x.Adm3Gid == adm3Gid.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(x => x.PricePerNight >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(x => x.PricePerNight <= maxPrice.Value);
        }

        if (guests.HasValue)
        {
            query = query.Where(x => x.MaxGuests >= guests.Value);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var normalizedTag = tag.Trim().ToLower();

            query = query.Where(x =>
                x.Tags.Any(t => t.Name.ToLower() == normalizedTag));
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

}
