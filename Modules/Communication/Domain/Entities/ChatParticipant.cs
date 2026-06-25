namespace Glinter.Modules.Communication.Domain.Entities;

public class ChatParticipant
{
    public Guid ThreadId { get; set; }

    public ChatThread Thread { get; set; } = null!;

    public Guid UserId { get; set; }

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastReadAtUtc { get; set; }

    public bool IsMuted { get; set; }

    public DateTime? LeftAtUtc { get; set; }
}
