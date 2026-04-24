using JetBrains.Annotations;
using Topaz.Service.Storage.Utils;

namespace Topaz.Service.Storage.Models;

[UsedImplicitly]
public class QueueMessage
{
    public QueueMessage()
    {
    }

    public QueueMessage(string id, string content)
    {
        Id = id;
        Content = content;
        PopReceipt = PopReceiptGenerator.Generate();
        EnqueuedTime = DateTimeOffset.UtcNow;
        DequeueCount = 0;
        VisibilityTimeout = 30;
        TimeToLive = 604800; // 7 days in seconds
    }

    /// <summary>
    /// Unique message identifier. Immutable after creation.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Base64-encoded message content. Maximum 64 KB when encoded.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Opaque token required to delete or update message visibility.
    /// Regenerated on each update.
    /// </summary>
    public string? PopReceipt { get; set; }

    /// <summary>
    /// Timestamp when message was first enqueued.
    /// </summary>
    public DateTimeOffset? EnqueuedTime { get; set; }

    /// <summary>
    /// Timestamp when message was last updated (visibility or content).
    /// </summary>
    public DateTimeOffset? UpdatedTime { get; set; }

    /// <summary>
    /// Number of times message has been dequeued/received.
    /// Increments on each dequeue operation.
    /// </summary>
    public int DequeueCount { get; set; }

    /// <summary>
    /// Seconds until message becomes visible again (0 = immediately visible).
    /// Range: 0-604,800 seconds (0-7 days).
    /// </summary>
    public int VisibilityTimeout { get; set; }

    /// <summary>
    /// Seconds until message automatically expires and is deleted.
    /// Default: 604,800 seconds (7 days).
    /// Range: 1-604,800 seconds.
    /// </summary>
    public int TimeToLive { get; set; }

    /// <summary>
    /// Timestamp when message becomes visible again.
    /// Null means message is currently visible.
    /// </summary>
    public DateTimeOffset? NextVisibleTime { get; set; }

    /// <summary>
    /// Timestamp when message will expire.
    /// Calculated as: EnqueuedTime + TimeToLive.
    /// </summary>
    public DateTimeOffset? ExpiryTime { get; set; }

    /// <summary>
    /// Update the visibility timeout and regenerate pop receipt.
    /// </summary>
    public void UpdateVisibility(int visibilityTimeout)
    {
        VisibilityTimeout = visibilityTimeout;
        UpdatedTime = DateTimeOffset.UtcNow;
        PopReceipt = PopReceiptGenerator.Generate();

        if (visibilityTimeout > 0)
        {
            NextVisibleTime = DateTimeOffset.UtcNow.AddSeconds(visibilityTimeout);
        }
        else
        {
            NextVisibleTime = null;
        }
    }

    /// <summary>
    /// Update message content and regenerate pop receipt.
    /// </summary>
    public void UpdateContent(string content)
    {
        Content = content;
        UpdatedTime = DateTimeOffset.UtcNow;
        PopReceipt = PopReceiptGenerator.Generate();
    }

    /// <summary>
    /// Check if message has expired based on TTL.
    /// </summary>
    public bool IsExpired()
    {
        if (ExpiryTime == null || EnqueuedTime == null)
            return false;

        return DateTimeOffset.UtcNow > ExpiryTime;
    }

    /// <summary>
    /// Check if message is currently visible (not in invisible period).
    /// </summary>
    public bool IsVisible()
    {
        if (NextVisibleTime == null)
            return true;

        return DateTimeOffset.UtcNow >= NextVisibleTime;
    }
}
