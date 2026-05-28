using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceCategories;

public class GetExperienceCategoriesQueryHandler
{
    private readonly IExperienceCategoryRepository _categoryRepository;

    public GetExperienceCategoriesQueryHandler(IExperienceCategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<List<ExperienceCategoryResponseDto>> HandleAsync(
        GetExperienceCategoriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetActiveAsync(cancellationToken);

        return categories
            .Select(ExperiencesMappings.ToCategoryResponse)
            .ToList();
    }
}