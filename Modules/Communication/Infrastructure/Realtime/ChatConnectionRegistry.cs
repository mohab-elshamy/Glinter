using System.Collections.Concurrent;

namespace Glinter.Modules.Communication.Infrastructure.Realtime;

public sealed class ChatConnectionRegistry
{
    private readonly ConcurrentDictionary<
        Guid,
        ConcurrentDictionary<string, Guid>> _connectionsByThread = new();

    public void Join(Guid threadId, string connectionId, Guid userId)
    {
        var connections = _connectionsByThread.GetOrAdd(
            threadId,
            _ => new ConcurrentDictionary<string, Guid>());
        connections[connectionId] = userId;
    }

    public void Leave(Guid threadId, string connectionId)
    {
        if (!_connectionsByThread.TryGetValue(threadId, out var connections))
            return;

        connections.TryRemove(connectionId, out _);
        if (connections.IsEmpty)
            _connectionsByThread.TryRemove(
                new KeyValuePair<Guid, ConcurrentDictionary<string, Guid>>(
                    threadId,
                    connections));
    }

    public void RemoveConnection(string connectionId)
    {
        foreach (var threadId in _connectionsByThread.Keys)
            Leave(threadId, connectionId);
    }

    public IReadOnlyList<string> GetConnections(
        Guid threadId,
        IReadOnlySet<Guid> allowedUserIds)
    {
        if (!_connectionsByThread.TryGetValue(threadId, out var connections))
            return [];

        return connections
            .Where(x => allowedUserIds.Contains(x.Value))
            .Select(x => x.Key)
            .ToArray();
    }
}
