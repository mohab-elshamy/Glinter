using Microsoft.AspNetCore.Authorization;
using Glinter.Modules.Stays.Application.Listings.Commands;
using Glinter.Modules.Stays.Application.Listings.Dtos;
using Glinter.Modules.Stays.Application.Listings.Queries;
using Microsoft.AspNetCore.Mvc;
namespace Glinter.Modules.Stays.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StaysController : ControllerBase
{

    private readonly CreateStayHandler _createStayHandler;
    private readonly GetAllStaysHandler _getAllStaysHandler;
    private readonly GetStayByIdHandler _getStayByIdHandler;
    private readonly UpdateStayHandler _updateStayHandler;
    private readonly GetStaysByNeighbourhoodHandler _getStaysByNeighbourhoodHandler;
    private readonly SetStayActiveStatusHandler _setStayActiveStatusHandler;
    public StaysController(
        CreateStayHandler createStayHandler,
        GetAllStaysHandler getAllStaysHandler,
        GetStayByIdHandler getStayByIdHandler,
        UpdateStayHandler updateStayHandler,
        GetStaysByNeighbourhoodHandler getStaysByNeighbourhoodHandler,
        SetStayActiveStatusHandler setStayActiveStatusHandler)
    {
        _createStayHandler = createStayHandler;
        _getAllStaysHandler = getAllStaysHandler;
        _getStayByIdHandler = getStayByIdHandler;
        _updateStayHandler = updateStayHandler;
        _getStaysByNeighbourhoodHandler = getStaysByNeighbourhoodHandler;
        _setStayActiveStatusHandler = setStayActiveStatusHandler;
    }
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStayRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateStayCommand
            {

                Adm3Gid = request.Adm3Gid,
                Name = request.Name,
                Description = request.Description,
                Address = request.Address,
                PricePerNight = request.PricePerNight,
                Currency = request.Currency,
                MaxGuests = request.MaxGuests,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Tags = request.Tags
            };

            var result = await _createStayHandler.HandleAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetStaysRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetAllStaysQuery
            {
                Adm3Gid = request.Adm3Gid,
                MinPrice = request.MinPrice,
                MaxPrice = request.MaxPrice,
                Guests = request.Guests,
                Tag = request.Tag
            };

            var result = await _getAllStaysHandler.HandleAsync(query, cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getStayByIdHandler.HandleAsync(new GetStayByIdQuery { Id = id }, cancellationToken);

        if (result is null)
            return NotFound(new { message = "Stay not found." });

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStayRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateStayCommand
            {
                Id = id,
                Name = request.Name,
                Description = request.Description,
                Address = request.Address,
                PricePerNight = request.PricePerNight,
                Currency = request.Currency,
                MaxGuests = request.MaxGuests,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Tags = request.Tags
            };

            var result = await _updateStayHandler.HandleAsync(command, cancellationToken);

            if (result is null)
                return NotFound(new { message = "Stay not found." });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("by-neighbourhood/{adm3Gid:int}")]
    public async Task<IActionResult> GetByNeighbourhood(int adm3Gid, CancellationToken cancellationToken)
    {
        var query = new GetStaysByNeighbourhoodQuery
        {
            Adm3Gid = adm3Gid
        };

        var result = await _getStaysByNeighbourhoodHandler.HandleAsync(query, cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var command = new SetStayActiveStatusCommand
        {
            StayId = id,
            IsActive = false
        };

        var result = await _setStayActiveStatusHandler.HandleAsync(command, cancellationToken);

        if (result is null)
            return NotFound(new { message = "Stay not found." });

        return Ok(result);
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var command = new SetStayActiveStatusCommand
        {
            StayId = id,
            IsActive = true
        };

        var result = await _setStayActiveStatusHandler.HandleAsync(command, cancellationToken);

        if (result is null)
            return NotFound(new { message = "Stay not found." });

        return Ok(result);
    }

}
