namespace Glinter.Modules.Stays.Application.Listings.Dtos;

public class StayTagDto
{
    public Guid Id { get; set; }
    public Guid StayId { get; set; }
    public string Name { get; set; } = string.Empty;
}