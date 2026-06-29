using System.Text.Json;

namespace Glinter.Shared.Application.Auditing;

public sealed class AdminAuditDetailsContext
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public string? ChangeDetailsJson { get; private set; }

    public void SetChanges<TBefore, TAfter>(TBefore before, TAfter after)
    {
        ChangeDetailsJson = JsonSerializer.Serialize(
            new
            {
                Before = before,
                After = after
            },
            SerializerOptions);
    }
}
