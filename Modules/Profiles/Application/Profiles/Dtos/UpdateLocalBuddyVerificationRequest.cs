namespace Glinter.Modules.Profiles.Application.Profiles.Dtos;

public class UpdateLocalBuddyVerificationRequest
{
    public string VerificationStatus { get; set; } = string.Empty;
    public string? ModerationNotes { get; set; }
}
