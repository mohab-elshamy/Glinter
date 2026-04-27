namespace Glinter.Modules.IdentityAccess.Domain.Entities;

public class RevokedToken
{
    public Guid Id { get; set; }
    public string Jti { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public DateTime RevokedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public string? Reason { get; set; }
}