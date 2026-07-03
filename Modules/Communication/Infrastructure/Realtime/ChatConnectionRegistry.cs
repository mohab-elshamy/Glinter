using System.Collections.Concurrent;

namespace Glinter.Modules.Communication.Infrastructure.Realtime;

public sealed class ChatConnectionRegistry
{
    private readonly ConcurrentDictionary<
        Guid,
        ConcurrentDictionary<string, ChatConnectionRegistration>> _connectionsByThread =
        new();
    private readonly ConcurrentDictionary<
        Guid,
        ConcurrentDictionary<string, ChatConnectionRegistration>> _connectionsByUser =
        new();

    public void Register(ChatConnectionRegistration registration)
    {
        var connections = _connectionsByUser.GetOrAdd(
            registration.UserId,
            _ => new ConcurrentDictionary<string, ChatConnectionRegistration>());
        connections[registration.ConnectionId] = registration;
    }

    public void Join(Guid threadId, ChatConnectionRegistration registration)
    {
        var connections = _connectionsByThread.GetOrAdd(
            threadId,
            _ => new ConcurrentDictionary<string, ChatConnectionRegistration>());
        connections[registration.ConnectionId] = registration;
    }

    public void Leave(Guid threadId, string connectionId)
    {
        if (!_connectionsByThread.TryGetValue(threadId, out var connections))
            return;

        connections.TryRemove(connectionId, out _);
        if (connections.IsEmpty)
            _connectionsByThread.TryRemove(
                new KeyValuePair<
                    Guid,
                    ConcurrentDictionary<string, ChatConnectionRegistration>>(
                    threadId,
                    connections));
    }

    public void RemoveConnection(string connectionId)
    {
        foreach (var threadId in _connectionsByThread.Keys)
            Leave(threadId, connectionId);

        foreach (var pair in _connectionsByUser)
        {
            pair.Value.TryRemove(connectionId, out _);
            if (pair.Value.IsEmpty)
            {
                _connectionsByUser.TryRemove(
                    new KeyValuePair<
                        Guid,
                        ConcurrentDictionary<string, ChatConnectionRegistration>>(
                        pair.Key,
                        pair.Value));
            }
        }
    }

    public IReadOnlyList<ChatConnectionRegistration> GetRegistrations(
        Guid threadId,
        IReadOnlySet<Guid> participantUserIds)
    {
        if (!_connectionsByThread.TryGetValue(threadId, out var connections))
            return [];

        return connections
            .Where(x => participantUserIds.Contains(x.Value.UserId))
            .Select(x => x.Value)
            .ToArray();
    }

    public IReadOnlyList<ChatConnectionRegistration> GetRegistrationsForUser(Guid userId)
    {
        return _connectionsByUser.TryGetValue(userId, out var connections)
            ? connections.Values.ToArray()
            : [];
    }
}

public sealed record ChatConnectionRegistration(
    string ConnectionId,
    Guid UserId,
    string Jti,
    string SecurityStamp,
    DateTime ExpiresAtUtc);
