namespace Glinter.Modules.IdentityAccess.Application.Auth.Dtos;

public class UserListItemResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Role { get; set; } = string.Empty;
    public string AccountReviewStatus { get; set; } = string.Empty;
    public string? IdentityDocumentUrl { get; set; }
    public string? IdentityDocumentFileName { get; set; }
    public string? IdentityDocumentContentType { get; set; }
    public string? AccountReviewNotes { get; set; }
    public Guid? AccountReviewedByUserId { get; set; }
    public DateTime? AccountReviewedAtUtc { get; set; }
}
