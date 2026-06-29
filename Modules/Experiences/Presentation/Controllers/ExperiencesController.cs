using System.Text.Json;
using Glinter.Modules.Experiences.Application.Dtos;
using Glinter.Modules.Experiences.Application.Services;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Experiences.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExperiencesController : ControllerBase
{
    private readonly ExperienceService _experienceService;

    public ExperiencesController(ExperienceService experienceService)
    {
        _experienceService = experienceService;
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
}
