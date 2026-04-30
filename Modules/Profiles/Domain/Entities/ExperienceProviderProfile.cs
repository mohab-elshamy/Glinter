namespace Glinter.Modules.Profiles.Domain.Entities;

public class ExperienceProviderProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string? ContactPersonName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Description { get; set; }
    
    public string? ProfileImageUrl { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}