using Amqp;
using Amqp.Listener;

namespace Topaz.Host.AMQP;

/// <summary>
/// Per-entity, per-session in-memory message queues and session state for AMQP session-based
/// messaging (requiresSession = true).
///
/// All access must be performed while holding <see cref="OutgoingLinkEndpoint.DeliveryLock"/>.
/// </summary>
internal static class SessionMessageStore
{
    // (entityAddress, sessionId) → message queue
    private static readonly Dictionary<(string, string), Queue<Message>> _queues =
        new(TupleComparer.Instance);

    // (entityAddress, sessionId) → session state bytes (null = no state set)
    private static readonly Dictionary<(string, string), byte[]?> _state =
        new(TupleComparer.Instance);

    // (entityAddress, sessionId) → (link holding the lock, lock expiry)
    private static readonly Dictionary<(string, string), (ListenerLink Link, DateTimeOffset Expiry)> _locks =
        new(TupleComparer.Instance);

    /// <summary>Enqueues a message for the given entity address and session ID.</summary>
    public static void Enqueue(string address, string sessionId, Message message)
    {
        var key = Key(address, sessionId);
        if (!_queues.TryGetValue(key, out var q))
        {
            q = new Queue<Message>();
            _queues[key] = q;
        }
        q.Enqueue(message);
    }

    /// <summary>
    /// Attempts to dequeue the next message for the given entity address and session ID.
    /// Returns <c>false</c> when the queue is empty or does not exist.
    /// </summary>
    public static bool TryDequeue(string address, string sessionId, out Message? message)
    {
        var key = Key(address, sessionId);
        if (_queues.TryGetValue(key, out var q) && q.Count > 0)
        {
            message = q.Dequeue();
            return true;
        }
        message = null;
        return false;
    }

    /// <summary>Returns the current queue depth for the given entity address and session ID.</summary>
    public static int Count(string address, string sessionId) =>
        _queues.TryGetValue(Key(address, sessionId), out var q) ? q.Count : 0;

    /// <summary>
    /// Returns any session ID that has queued messages and no active lock, or <c>null</c>
    /// when no such session exists.  Used for wildcard (null) session-filter receivers.
    /// </summary>
    public static string? GetNextAvailableSession(string address)
    {
        var normalizedAddress = Normalize(address);
        foreach (var (key, queue) in _queues)
        {
            if (!string.Equals(key.Item1, normalizedAddress, StringComparison.OrdinalIgnoreCase))
                continue;
            if (queue.Count == 0)
                continue;
            if (_locks.ContainsKey(key))
                continue;
            return key.Item2;
        }
        return null;
    }

    /// <summary>
    /// Attempts to acquire the session lock for the given entity and session ID.
    /// Returns <c>false</c> if the session is already locked by a different link.
    /// </summary>
    public static bool TryAcquireSessionLock(string address, string sessionId, ListenerLink link)
    {
        var key = Key(address, sessionId);
        if (_locks.TryGetValue(key, out var existing))
        {
            // Allow re-acquisition by the same link (idempotent flow re-entry).
            return ReferenceEquals(existing.Link, link);
        }
        _locks[key] = (link, DateTimeOffset.UtcNow.AddMinutes(5));
        return true;
    }

    /// <summary>Releases the session lock for the given entity and session ID.</summary>
    public static void ReleaseSessionLock(string address, string sessionId)
    {
        _locks.Remove(Key(address, sessionId));
    }

    /// <summary>
    /// Renews the session lock expiry and returns the new expiry time.
    /// Creates the lock entry if it does not exist (covers management renew before flow).
    /// </summary>
    public static DateTimeOffset RenewSessionLock(string address, string sessionId)
    {
        var key = Key(address, sessionId);
        var expiry = DateTimeOffset.UtcNow.AddMinutes(5);
        if (_locks.TryGetValue(key, out var existing))
            _locks[key] = (existing.Link, expiry);
        return expiry;
    }

    /// <summary>Returns <c>true</c> if the given session is currently locked by any receiver.</summary>
    public static bool IsSessionLocked(string address, string sessionId) =>
        _locks.ContainsKey(Key(address, sessionId));

    /// <summary>Returns the session state for the given entity and session ID, or <c>null</c> if not set.</summary>
    public static byte[]? GetSessionState(string address, string sessionId) =>
        _state.TryGetValue(Key(address, sessionId), out var s) ? s : null;

    /// <summary>Sets (or clears when <paramref name="state"/> is <c>null</c>) the session state.</summary>
    public static void SetSessionState(string address, string sessionId, byte[]? state)
    {
        var key = Key(address, sessionId);
        if (state is null)
            _state.Remove(key);
        else
            _state[key] = state;
    }

    /// <summary>
    /// Removes all queued messages, session state, and locks for the given entity address.
    /// Called when a queue is (re)created to discard stale data from a previous lifetime.
    /// </summary>
    public static void ClearQueue(string address)
    {
        var normalized = Normalize(address);
        foreach (var key in _queues.Keys.Where(k => string.Equals(k.Item1, normalized, StringComparison.OrdinalIgnoreCase)).ToList())
            _queues.Remove(key);
        foreach (var key in _state.Keys.Where(k => string.Equals(k.Item1, normalized, StringComparison.OrdinalIgnoreCase)).ToList())
            _state.Remove(key);
        foreach (var key in _locks.Keys.Where(k => string.Equals(k.Item1, normalized, StringComparison.OrdinalIgnoreCase)).ToList())
            _locks.Remove(key);
    }

    private static (string, string) Key(string address, string sessionId) =>
        (Normalize(address), sessionId);

    private static string Normalize(string address) => address.Trim('/').ToLowerInvariant();

    // Case-insensitive tuple comparer for the (address, sessionId) dictionary keys.
    private sealed class TupleComparer : IEqualityComparer<(string, string)>
    {
        public static readonly TupleComparer Instance = new();
        public bool Equals((string, string) x, (string, string) y) =>
            string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Item2, y.Item2, StringComparison.Ordinal);
        public int GetHashCode((string, string) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1),
                StringComparer.Ordinal.GetHashCode(obj.Item2));
    }
}
