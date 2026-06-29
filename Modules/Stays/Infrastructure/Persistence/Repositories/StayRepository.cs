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
        return await ExistsAsync(ownerProfileId, name, address, Guid.Empty, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid ownerProfileId,
        string name,
        string address,
        Guid excludedStayId,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        var normalizedAddress = address.Trim().ToLower();

        return await _dbContext.Stays.AnyAsync(
            x => x.OwnerProfileId == ownerProfileId
                 && x.Id != excludedStayId
                 && x.Name.ToLower() == normalizedName
                 && x.Address.ToLower() == normalizedAddress,
            cancellationToken);
    }

    public async Task ReplaceTagsAsync(
        Guid stayId,
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default)
    {
        var existingTags = await _dbContext.StayTags
            .Where(x => x.StayId == stayId)
            .ToListAsync(cancellationToken);

        _dbContext.StayTags.RemoveRange(existingTags);

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

    public async Task<List<Stay>> GetByAdm3GidAsync(
        int adm3Gid,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedPageSize) = NormalizePagination(page, pageSize);

        return await _dbContext.Stays
            .Include(x => x.Tags)
            .Where(x => x.Adm3Gid == adm3Gid && x.IsActive)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Stay>> GetFilteredAsync(
        int? adm3Gid,
        decimal? minPrice,
        decimal? maxPrice,
        int? guests,
        string? tag,
        string? search,
        string? currency,
        DateOnly? checkInDate,
        DateOnly? checkOutDate,
        string sortBy,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedPageSize) = NormalizePagination(page, pageSize);

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

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = CreateContainsPattern(search);
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, searchPattern, @"\") ||
                EF.Functions.ILike(x.Description, searchPattern, @"\") ||
                EF.Functions.ILike(x.Address, searchPattern, @"\") ||
                x.Tags.Any(t => EF.Functions.ILike(t.Name, searchPattern, @"\")));
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            var normalizedCurrency = currency.Trim().ToUpper();
            query = query.Where(x => x.Currency == normalizedCurrency);
        }

        if (checkInDate.HasValue && checkOutDate.HasValue)
        {
            query = query.Where(x => !x.Bookings.Any(booking =>
                booking.Status != "Cancelled" &&
                booking.CheckInDate < checkOutDate.Value &&
                booking.CheckOutDate > checkInDate.Value));
        }

        query = sortBy switch
        {
            "price_asc" => query
                .OrderBy(x => x.PricePerNight)
                .ThenBy(x => x.Id),
            "price_desc" => query
                .OrderByDescending(x => x.PricePerNight)
                .ThenBy(x => x.Id),
            _ => query
                .OrderByDescending(x => x.CreatedAtUtc)
                .ThenBy(x => x.Id)
        };

        return await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);
    }

    private static (int Page, int PageSize) NormalizePagination(int page, int pageSize)
    {
        var normalizedPage = page <= 0 ? 1 : Math.Min(page, 10000);
        var normalizedPageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        return (normalizedPage, normalizedPageSize);
    }

    private static string CreateContainsPattern(string value)
    {
        var escaped = value.Trim()
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_");
        return $"%{escaped}%";
    }
}
