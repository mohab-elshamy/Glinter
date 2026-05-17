using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceCategories;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Experiences.Presentation.Controllers;

[ApiController]
[Route("api/experience-categories")]
public class ExperienceCategoriesController : ControllerBase
{
    private readonly GetExperienceCategoriesQueryHandler _getCategoriesHandler;

    public ExperienceCategoriesController(GetExperienceCategoriesQueryHandler getCategoriesHandler)
    {
        _getCategoriesHandler = getCategoriesHandler;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExperienceCategoryResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _getCategoriesHandler.HandleAsync(
            new GetExperienceCategoriesQuery(),
            cancellationToken);

        return Ok(result);
    }
}