using Glinter.Modules.Stays.Domain.Entities;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Stays.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StaysController : ControllerBase
{
    private readonly GlinterDbContext _dbContext;

    public StaysController(GlinterDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var stays = await _dbContext.Stays
            .Include(x => x.Tags)
            .Include(x => x.Reviews)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        return Ok(stays);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var stay = await _dbContext.Stays
            .Include(x => x.Tags)
            .Include(x => x.Reviews)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (stay is null)
            return NotFound(new { message = "Stay not found." });

        return Ok(stay);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStayRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        if (request.PricePerNight <= 0)
            return BadRequest(new { message = "PricePerNight must be greater than 0." });

        if (request.MaxGuests <= 0)
            return BadRequest(new { message = "MaxGuests must be greater than 0." });

        var stay = new Stay
        {
            Id = Guid.NewGuid(),
            OwnerProfileId = request.OwnerProfileId,
            AreaId = request.AreaId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Address = request.Address?.Trim() ?? string.Empty,
            PricePerNight = request.PricePerNight,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EGP" : request.Currency.Trim(),
            MaxGuests = request.MaxGuests,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        if (request.Tags is not null && request.Tags.Count > 0)
        {
            stay.Tags = request.Tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => new StayTag
                {
                    Id = Guid.NewGuid(),
                    StayId = stay.Id,
                    Name = t.Trim()
                })
                .ToList();
        }

        _dbContext.Stays.Add(stay);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = stay.Id }, stay);
    }
}

public class CreateStayRequest
{
    public Guid OwnerProfileId { get; set; }
    public Guid AreaId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }

    public decimal PricePerNight { get; set; }
    public string Currency { get; set; } = "EGP";
    public int MaxGuests { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public List<string>? Tags { get; set; }
}