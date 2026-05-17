namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class ExperienceCategoryResponseDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}