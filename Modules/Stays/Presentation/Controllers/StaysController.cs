using System.ComponentModel.DataAnnotations;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Stays.Application.Listings.Commands;
using Glinter.Modules.Stays.Application.Listings.Dtos;
using Glinter.Modules.Stays.Application.Listings.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

    [Authorize(Roles = RoleNames.HotelOwner)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStayRequestDto request, CancellationToken cancellationToken)
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

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetStaysRequestDto request,
        CancellationToken cancellationToken)
    {
        var query = new GetAllStaysQuery
        {
            Adm3Gid = request.Adm3Gid,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            Guests = request.Guests,
            Tag = request.Tag,
            Search = request.Search,
            Currency = request.Currency,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            SortBy = request.SortBy,
            Page = request.Page,
            PageSize = request.PageSize
        };

        var result = await _getAllStaysHandler.HandleAsync(query, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getStayByIdHandler.HandleAsync(new GetStayByIdQuery { Id = id }, cancellationToken);

        if (result is null)
            throw new NotFoundException("Stay not found.");

        return Ok(result);
    }

    [Authorize(Roles = RoleNames.HotelOwner)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStayRequestDto request, CancellationToken cancellationToken)
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
            throw new NotFoundException("Stay not found.");

        return Ok(result);
    }

    [HttpGet("by-neighbourhood/{adm3Gid:int}")]
    public async Task<IActionResult> GetByNeighbourhood(
        int adm3Gid,
        [FromQuery, Range(1, 10000, ErrorMessage = "Page must be between 1 and 10000.")] int page = 1,
        [FromQuery, Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetStaysByNeighbourhoodQuery
        {
            Adm3Gid = adm3Gid,
            Page = page,
            PageSize = pageSize
        };

        var result = await _getStaysByNeighbourhoodHandler.HandleAsync(query, cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = RoleNames.HotelOwner)]
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
            throw new NotFoundException("Stay not found.");

        return Ok(result);
    }

    [Authorize(Roles = RoleNames.HotelOwner)]
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
            throw new NotFoundException("Stay not found.");

        return Ok(result);
    }
}
