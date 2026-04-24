namespace Glinter.Modules.Stays.Domain.Entities;

public class StayTag
{
    public Guid Id { get; set; }

    public Guid StayId { get; set; }
    public Stay Stay { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
}