namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpdateLocalBuddyVerification;

public class UpdateLocalBuddyVerificationCommand
{
    public Guid UserId { get; set; }

    public string VerificationStatus { get; set; } = string.Empty;
    public string? ModerationNotes { get; set; }
}
