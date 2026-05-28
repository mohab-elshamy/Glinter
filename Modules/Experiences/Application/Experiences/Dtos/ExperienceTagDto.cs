namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class ExperienceTagDto
{
    public Guid Id { get; set; }

    public Guid ExperienceId { get; set; }

    public string Name { get; set; } = string.Empty;
}