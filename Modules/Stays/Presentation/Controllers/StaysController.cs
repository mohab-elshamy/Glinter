using System.Text.Json;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Stays.Application.Dtos;
using Glinter.Modules.Stays.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Glinter.Modules.Stays.Infrastructure.Files;

namespace Glinter.Modules.Stays.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StaysController : ControllerBase
{
    private readonly StayService _stayService;
    private readonly StayImageStorage _imageStorage;

    public StaysController(StayService stayService, StayImageStorage imageStorage)
    {
        _stayService = stayService;
        _imageStorage = imageStorage;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] StayListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _stayService.GetStaysAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("region-stats")]
    public async Task<IActionResult> GetRegionStats(
        [FromQuery] StayRegionStatsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _stayService.GetRegionStatsAsync(request, cancellationToken);
        return Ok(result);
    }

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.HotelOwner + "," + RoleNames.Admin)]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await _stayService.GetMyStaysAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _stayService.GetByIdAsync(id, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Stay not found." });
        }

        return Ok(result);
    }

    [HttpGet("{id:int}/reviews")]
    public async Task<IActionResult> GetReviews(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _stayService.GetReviewsAsync(id, page, pageSize, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Stay not found." });
        }

        return Ok(result);
    }

    [HttpGet("{id:int}/reviews/llm-input")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.Admin)]
    public async Task<IActionResult> GetReviewsForLlm(int id, CancellationToken cancellationToken)
    {
        var result = await _stayService.GetReviewsForLlmAsync(id, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Stay not found." });
        }

        return Ok(result);
    }

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.HotelOwner + "," + RoleNames.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateStayRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stayService.CreateOwnerStayAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.HotelOwner + "," + RoleNames.Admin)]
    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(StayImageStorage.MaxImageBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = StayImageStorage.MaxImageBytes)]
    public async Task<IActionResult> UploadImage(
        [FromForm] UploadStayImageRequest request,
        CancellationToken cancellationToken)
    {
        var stored = await _imageStorage.SaveAsync(request.File, cancellationToken);
        return Ok(new StayImageUploadResponse
        {
            FileName = stored.FileName,
            SizeBytes = stored.SizeBytes,
            Link = Url.ActionLink(
                nameof(GetImage),
                values: new { fileName = stored.FileName })!
        });
    }

    [AllowAnonymous]
    [HttpGet("images/{fileName}")]
    public IActionResult GetImage(string fileName)
    {
        var stored = _imageStorage.Find(fileName);
        return stored is null
            ? NotFound()
            : PhysicalFile(stored.Value.Path, stored.Value.ContentType);
    }

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.HotelOwner + "," + RoleNames.Admin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateStayRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stayService.UpdateOwnerStayAsync(id, request, cancellationToken);
            return result is null ? NotFound(new { message = "Stay not found." }) : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.HotelOwner + "," + RoleNames.Admin)]
    [HttpPatch("{id:int}/activate")]
    public Task<IActionResult> Activate(int id, CancellationToken cancellationToken) =>
        SetActive(id, true, cancellationToken);

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.HotelOwner + "," + RoleNames.Admin)]
    [HttpPatch("{id:int}/deactivate")]
    public Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken) =>
        SetActive(id, false, cancellationToken);

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.Traveler)]
    [HttpPost("{id:int}/bookings")]
    public async Task<IActionResult> CreateBooking(
        int id,
        [FromBody] CreateStayBookingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stayService.CreateBookingAsync(id, request, cancellationToken);
            return result is null
                ? NotFound(new { message = "Active stay not found." })
                : StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.HotelOwner + "," + RoleNames.Admin)]
    [HttpGet("{id:int}/bookings")]
    public async Task<IActionResult> GetBookings(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stayService.GetStayBookingsAsync(id, cancellationToken);
            return result is null ? NotFound(new { message = "Stay not found." }) : Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.Traveler)]
    [HttpPost("{id:int}/reviews")]
    public async Task<IActionResult> CreateReview(
        int id,
        [FromBody] CreateStayReviewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stayService.CreateGlinterReviewAsync(id, request, cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Stay not found." });
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.Admin)]
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportFile(
        [FromForm] ImportStaysFileRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new { message = "JSON file is required." });
        }

        try
        {
            await using var stream = request.File.OpenReadStream();
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var items = ResolveImportPayload(document.RootElement);
            var result = await _stayService.ImportThirdPartyAsync(items, cancellationToken);

            return Ok(result);
        }
        catch (JsonException)
        {
            return BadRequest(new { message = "Uploaded file must contain valid JSON." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static List<JsonElement> ResolveImportPayload(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("Uploaded JSON file must contain an array of stays.");
        }

        var items = body.EnumerateArray().Select(x => x.Clone()).ToList();

        if (items.Count == 0)
        {
            throw new ArgumentException("Import file must contain at least one stay.");
        }

        return items;
    }

    private async Task<IActionResult> SetActive(
        int id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stayService.SetActiveAsync(id, isActive, cancellationToken);
            return result is null ? NotFound(new { message = "Stay not found." }) : Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
