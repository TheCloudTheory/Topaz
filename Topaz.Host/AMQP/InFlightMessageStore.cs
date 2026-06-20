using Amqp;
using Amqp.Framing;
using Amqp.Types;

namespace Topaz.Host.AMQP;

/// <summary>
/// Tracks messages that have been delivered to consumers under PeekLock mode but not yet
/// settled.  Supports lookup both by lock-token GUID (used by management operations such as
/// com.microsoft:complete/abandon/dead-letter) and by message reference (used by regular
/// DISPOSITION frames from the consumer link).
///
/// All access must be performed while holding <see cref="OutgoingLinkEndpoint.DeliveryLock"/>.
/// </summary>
internal static class InFlightMessageStore
{
    // Primary index: lock-token (Guid) → (entityAddress, message)
    private static readonly Dictionary<Guid, (string EntityAddress, Message Message)> _byLockToken = new();

    // Reverse index: message reference → lock-token, for disposition callbacks where only the
    // Message object is available (AMQPNetLite resolves the Message from the delivery-id).
    private static readonly Dictionary<Message, Guid> _byMessage =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>Registers a newly-delivered message under the given lock token.</summary>
    internal static void Track(Guid lockToken, string entityAddress, Message message)
    {
        _byLockToken[lockToken] = (entityAddress, message);
        _byMessage[message] = lockToken;
    }

    /// <summary>
    /// Removes a message from in-flight tracking by lock token (management complete).
    /// Returns <c>false</c> if the token is unknown (e.g. already settled).
    /// </summary>
    internal static bool TryCompleteByLockToken(Guid lockToken)
    {
        if (!_byLockToken.Remove(lockToken, out var entry))
            return false;
        _byMessage.Remove(entry.Message);
        return true;
    }

    /// <summary>
    /// Removes a message from in-flight tracking by message reference (DISPOSITION complete).
    /// Returns <c>false</c> if the message is unknown.
    /// </summary>
    internal static bool TryCompleteByMessage(Message message)
    {
        if (!_byMessage.Remove(message, out var lockToken))
            return false;
        _byLockToken.Remove(lockToken);
        return true;
    }

    /// <summary>
    /// Abandons a message identified by lock token: increments delivery count and
    /// re-enqueues it, or routes to the dead-letter sub-queue when
    /// <paramref name="maxDeliveryCount"/> is exceeded.
    /// </summary>
    internal static void HandleAbandonByLockToken(Guid lockToken, int maxDeliveryCount)
    {
        if (!_byLockToken.TryGetValue(lockToken, out var entry))
            return;
        TryCompleteByLockToken(lockToken);
        AbandonCore(entry.EntityAddress, entry.Message, maxDeliveryCount);
    }

    /// <summary>
    /// Abandons a message identified by message reference (regular DISPOSITION Released).
    /// </summary>
    internal static void HandleAbandonByMessage(Message message, int maxDeliveryCount)
    {
        if (!_byMessage.TryGetValue(message, out var lockToken))
            return;
        var entityAddress = _byLockToken[lockToken].EntityAddress;
        TryCompleteByMessage(message);
        AbandonCore(entityAddress, message, maxDeliveryCount);
    }

    /// <summary>
    /// Immediately dead-letters a message identified by lock token.
    /// </summary>
    internal static void HandleDeadLetterByLockToken(Guid lockToken, string? reason, string? description)
    {
        if (!_byLockToken.TryGetValue(lockToken, out var entry))
            return;
        TryCompleteByLockToken(lockToken);
        DeadLetterCore(entry.EntityAddress, entry.Message, reason, description);
    }

    /// <summary>
    /// Immediately dead-letters a message identified by message reference (DISPOSITION Rejected).
    /// </summary>
    internal static void HandleDeadLetterByMessage(Message message, string? reason, string? description)
    {
        if (!_byMessage.TryGetValue(message, out var lockToken))
            return;
        var entityAddress = _byLockToken[lockToken].EntityAddress;
        TryCompleteByMessage(message);
        DeadLetterCore(entityAddress, message, reason, description);
    }

    private static void AbandonCore(string entityAddress, Message message, int maxDeliveryCount)
    {
        message.Header ??= new Header();
        var newCount = message.Header.DeliveryCount + 1u;
        message.Header.DeliveryCount = newCount;

        if (newCount >= (uint)maxDeliveryCount)
        {
            DeadLetterCore(entityAddress, message,
                "MaxDeliveryCountExceeded",
                $"Message could not be consumed after {newCount} delivery attempt(s).");
        }
        else
        {
            SubscriptionMessageStore.Enqueue(entityAddress, message);
            OutgoingLinkEndpoint.NotifyMessageEnqueued();
        }
    }

    private static void DeadLetterCore(string entityAddress, Message message, string? reason, string? description)
    {
        message.MessageAnnotations[new Symbol("x-opt-deadletter-reason")] = reason ?? "Unspecified";
        message.MessageAnnotations[new Symbol("x-opt-deadletter-error-description")] = description ?? string.Empty;

        var dlqAddress = $"{SubscriptionMessageStore.Normalize(entityAddress)}/$deadletterqueue";
        SubscriptionMessageStore.Enqueue(dlqAddress, message);
        OutgoingLinkEndpoint.NotifyMessageEnqueued();
    }
}
