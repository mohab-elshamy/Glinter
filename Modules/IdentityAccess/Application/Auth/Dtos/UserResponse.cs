namespace Glinter.Modules.IdentityAccess.Application.Auth.Dtos;

public class UserResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<string> Roles { get; set; } = [];
    public string AccountReviewStatus { get; set; } = string.Empty;
    public string? IdentityDocumentUrl { get; set; }
    public string? IdentityDocumentFileName { get; set; }
    public string? IdentityDocumentContentType { get; set; }
    public string? AccountReviewNotes { get; set; }
    public Guid? AccountReviewedByUserId { get; set; }
    public DateTime? AccountReviewedAtUtc { get; set; }
}
