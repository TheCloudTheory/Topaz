using Amqp;

namespace Topaz.Host.AMQP;

/// <summary>
/// Per-entity in-memory message queues, replacing the single global queue that all
/// producers and consumers previously shared.  Each AMQP entity address (normalized
/// to lowercase) has its own queue so topic fan-out and queue isolation work correctly.
///
/// All access must be performed while holding <see cref="OutgoingLinkEndpoint.DeliveryLock"/>.
/// </summary>
internal static class SubscriptionMessageStore
{
    private static readonly Dictionary<string, Queue<Message>> _queues =
        new(StringComparer.OrdinalIgnoreCase);

    // Global monotonically-increasing offset counter, used for the x-opt-offset annotation.
    private static long _nextOffset;

    /// <summary>Enqueues a message for the given entity address.</summary>
    public static void Enqueue(string address, Message message)
    {
        var key = Normalize(address);
        if (!_queues.TryGetValue(key, out var q))
        {
            q = new Queue<Message>();
            _queues[key] = q;
        }
        q.Enqueue(message);
    }

    /// <summary>
    /// Attempts to dequeue the next message for the given entity address.
    /// Returns <c>false</c> when the queue is empty or does not exist.
    /// </summary>
    public static bool TryDequeue(string address, out Message? message)
    {
        var key = Normalize(address);
        if (_queues.TryGetValue(key, out var q) && q.Count > 0)
        {
            message = q.Dequeue();
            return true;
        }
        message = null;
        return false;
    }

    /// <summary>Returns the current queue depth for the given entity address.</summary>
    public static int Count(string address)
    {
        var key = Normalize(address);
        return _queues.TryGetValue(key, out var q) ? q.Count : 0;
    }

    /// <summary>
    /// Ensures an entity queue slot exists (creates an empty queue if absent).
    /// Calling this before a consumer attaches allows pre-consumer messages to accumulate.
    /// </summary>
    public static void EnsureQueue(string address)
    {
        var key = Normalize(address);
        if (!_queues.ContainsKey(key))
            _queues[key] = new Queue<Message>();
    }

    /// <summary>Returns the next global message offset (thread-safe, no lock required).</summary>
    public static long NextOffset() => Interlocked.Increment(ref _nextOffset) - 1;

    /// <summary>Normalizes an AMQP entity address to a stable dictionary key.</summary>
    public static string Normalize(string address) => address.Trim('/').ToLowerInvariant();
}
