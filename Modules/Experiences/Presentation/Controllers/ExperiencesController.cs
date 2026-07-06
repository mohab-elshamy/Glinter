using System.Text.Json;
using Glinter.Modules.Experiences.Application.Dtos;
using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Services;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Experiences.Infrastructure.Files;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Glinter.Modules.Experiences.Infrastructure.DependencyInjection;

namespace Glinter.Modules.Experiences.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExperiencesController : ControllerBase
{
    private readonly ExperienceService _experienceService;
    private readonly IExperienceRecommendationService _recommendationService;
    private readonly ExperienceImageStorage _imageStorage;

    public ExperiencesController(
        ExperienceService experienceService,
        IExperienceRecommendationService recommendationService,
        ExperienceImageStorage imageStorage)
    {
        _experienceService = experienceService;
        _recommendationService = recommendationService;
        _imageStorage = imageStorage;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] ExperienceListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _experienceService.GetExperiencesAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("map")]
    public async Task<IActionResult> GetMapItems(
        [FromQuery] ExperienceListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _experienceService.GetMapItemsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        return Ok(Enum.GetNames<ExperienceCategory>());
    }

    [HttpPost("recommendations")]
    [EnableRateLimiting(ExperienceRecommendationRateLimitPolicies.AiRequests)]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> Recommend(
        [FromBody] ExperienceRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _recommendationService.RecommendAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return RecommendationValidationProblem(ex);
        }
    }

    [HttpPost("recommendations/natural-language")]
    [EnableRateLimiting(ExperienceRecommendationRateLimitPolicies.AiRequests)]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> RecommendFromNaturalLanguage(
        [FromBody] NaturalLanguageExperienceRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _recommendationService.RecommendFromTextAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return RecommendationValidationProblem(ex);
        }
    }

    private IActionResult RecommendationValidationProblem(ArgumentException exception)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid recommendation request.",
            Detail = exception.Message,
            Instance = Request.Path
        };
        problem.Extensions["errorCode"] = "invalid_recommendation_request";
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return BadRequest(problem);
    }

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.ExperienceProvider + "," + RoleNames.Admin)]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await _experienceService.GetMyExperiencesAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _experienceService.GetByIdAsync(id, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Experience not found." });
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
        var result = await _experienceService.GetReviewsAsync(id, page, pageSize, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Experience not found." });
        }

        return Ok(result);
    }

    [HttpGet("{id:int}/reviews/llm-input")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.Admin)]
    public async Task<IActionResult> GetReviewsForLlm(int id, CancellationToken cancellationToken)
    {
        var result = await _experienceService.GetReviewsForLlmAsync(id, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Experience not found." });
        }

        return Ok(result);
    }

    [HttpGet("{id:int}/visit-insights")]
    public async Task<IActionResult> GetVisitInsights(
        int id,
        [FromQuery] DateTime? visitAt,
        CancellationToken cancellationToken)
    {
        var result = await _experienceService.GetVisitInsightAsync(
            id,
            visitAt ?? DateTime.Now,
            cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Experience not found." });
        }

        return Ok(result);
    }

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.ExperienceProvider + "," + RoleNames.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateExperienceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _experienceService.CreateProviderExperienceAsync(request, cancellationToken);
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
        Roles = RoleNames.ExperienceProvider + "," + RoleNames.Admin)]
    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ExperienceImageStorage.MaxImageBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ExperienceImageStorage.MaxImageBytes)]
    public async Task<IActionResult> UploadImage(
        [FromForm] UploadExperienceImageRequest request,
        CancellationToken cancellationToken)
    {
        var stored = await _imageStorage.SaveAsync(request.File, cancellationToken);
        return Ok(new ExperienceImageUploadResponse
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
        Roles = RoleNames.ExperienceProvider + "," + RoleNames.Admin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateExperienceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _experienceService.UpdateProviderExperienceAsync(id, request, cancellationToken);
            return result is null ? NotFound(new { message = "Experience not found." }) : Ok(result);
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
        Roles = RoleNames.ExperienceProvider + "," + RoleNames.Admin)]
    [HttpPatch("{id:int}/activate")]
    public Task<IActionResult> Activate(int id, CancellationToken cancellationToken) =>
        SetActive(id, true, cancellationToken);

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.ExperienceProvider + "," + RoleNames.Admin)]
    [HttpPatch("{id:int}/deactivate")]
    public Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken) =>
        SetActive(id, false, cancellationToken);

    [HttpGet("{id:int}/availability")]
    public async Task<IActionResult> GetAvailability(int id, CancellationToken cancellationToken)
    {
        var result = await _experienceService.GetAvailabilityAsync(id, false, cancellationToken);
        return result is null ? NotFound(new { message = "Active experience not found." }) : Ok(result);
    }

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.ExperienceProvider + "," + RoleNames.Admin)]
    [HttpGet("{id:int}/availability/manage")]
    public async Task<IActionResult> GetManagedAvailability(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _experienceService.GetAvailabilityAsync(id, true, cancellationToken);
            return result is null ? NotFound(new { message = "Experience not found." }) : Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.ExperienceProvider + "," + RoleNames.Admin)]
    [HttpPost("{id:int}/availability")]
    public async Task<IActionResult> CreateAvailability(
        int id,
        [FromBody] CreateExperienceAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _experienceService.CreateAvailabilityAsync(id, request, cancellationToken);
            return result is null
                ? NotFound(new { message = "Experience not found." })
                : StatusCode(StatusCodes.Status201Created, result);
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

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.Traveler)]
    [HttpPost("{id:int}/bookings")]
    public async Task<IActionResult> CreateBooking(
        int id,
        [FromBody] CreateExperienceBookingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _experienceService.CreateBookingAsync(id, request, cancellationToken);
            return result is null
                ? NotFound(new { message = "Bookable experience or availability not found." })
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
        Roles = RoleNames.ExperienceProvider + "," + RoleNames.Admin)]
    [HttpGet("{id:int}/bookings")]
    public async Task<IActionResult> GetBookings(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _experienceService.GetExperienceBookingsAsync(id, cancellationToken);
            return result is null ? NotFound(new { message = "Experience not found." }) : Ok(result);
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
        [FromBody] CreateExperienceReviewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _experienceService.CreateReviewAsync(id, request, cancellationToken);
            return result is null ? NotFound(new { message = "Active experience not found." }) : Ok(result);
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
        [FromForm] ImportExperiencesFileRequest request,
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

            var items = ResolveImportPayload(request.Category, document.RootElement);
            var result = await _experienceService.ImportThirdPartyAsync(request.Category!.Value, items, cancellationToken);

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

    private static List<JsonElement> ResolveImportPayload(
        ExperienceCategory? category,
        JsonElement body)
    {
        if (category is null)
        {
            throw new ArgumentException("Category is required.");
        }

        if (body.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("Uploaded JSON file must contain an array of experiences.");
        }

        var items = body.EnumerateArray().Select(x => x.Clone()).ToList();

        if (items.Count == 0)
        {
            throw new ArgumentException("Import file must contain at least one experience.");
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
            var result = await _experienceService.SetActiveAsync(id, isActive, cancellationToken);
            return result is null ? NotFound(new { message = "Experience not found." }) : Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
