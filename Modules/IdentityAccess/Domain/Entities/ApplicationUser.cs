using Microsoft.AspNetCore.Identity;
using Glinter.Modules.IdentityAccess.Domain.Enums;

namespace Glinter.Modules.IdentityAccess.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public AccountReviewStatus AccountReviewStatus { get; set; } = AccountReviewStatus.NotRequired;
    public string? IdentityDocumentUrl { get; set; }
    public string? IdentityDocumentFileName { get; set; }
    public string? IdentityDocumentContentType { get; set; }
    public string? AccountReviewNotes { get; set; }
    public Guid? AccountReviewedByUserId { get; set; }
    public DateTime? AccountReviewedAtUtc { get; set; }
}
