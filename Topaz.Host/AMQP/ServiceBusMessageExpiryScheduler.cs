using Amqp;
using Amqp.Framing;
using Amqp.Types;
using Topaz.Service.ServiceBus.Filtering;
using Topaz.Shared;

namespace Topaz.Host.AMQP;

/// <summary>
/// Background scheduler that periodically scans <see cref="SubscriptionMessageStore"/> and
/// evicts messages whose TTL has elapsed.  Expired messages are either routed to the entity's
/// <c>$deadletterqueue</c> (when <c>DeadLetteringOnMessageExpiration=true</c>) or silently
/// discarded (when <c>false</c>, which is the default).
/// </summary>
internal sealed class ServiceBusMessageExpiryScheduler(
    ServiceBusRuleLoader ruleLoader,
    ITopazLogger logger,
    TimeSpan interval) : ITopazBackgroundService
{
    public string Name => $"Service Bus — message TTL expiry (interval: {interval})";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug(nameof(ServiceBusMessageExpiryScheduler), nameof(StartAsync),
            "Message expiry scheduler started (interval: {0})", interval);

        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
                ScanAndExpire();
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown — exit gracefully
        }
    }

    internal void ScanAndExpire()
    {
        IReadOnlyCollection<string> addresses;
        lock (OutgoingLinkEndpoint.DeliveryLock)
            addresses = SubscriptionMessageStore.GetAllAddresses();

        var now = DateTime.UtcNow;

        foreach (var address in addresses)
        {
            // Only scan primary queues; DLQs don't expire further.
            if (address.EndsWith("/$deadletterqueue", StringComparison.OrdinalIgnoreCase))
                continue;

            var entityTtl = ruleLoader.GetDefaultMessageTimeToLive(address);
            var deadLetter = ruleLoader.GetDeadLetteringOnMessageExpiration(address);

            bool IsExpired(Message msg)
            {
                if (!msg.MessageAnnotations.Map.TryGetValue(new Symbol("x-opt-enqueued-time-utc"), out var enqObj)
                    || enqObj is not DateTime enqTime)
                    return false; // no enqueue stamp → not eligible

                // Per-message TTL wins if set; otherwise fall back to entity default.
                var effectiveTtl = msg.Header?.Ttl > 0
                    ? TimeSpan.FromMilliseconds(msg.Header.Ttl)
                    : entityTtl;

                return effectiveTtl < TimeSpan.MaxValue && (enqTime + effectiveTtl) < now;
            }

            List<Message> expired;
            lock (OutgoingLinkEndpoint.DeliveryLock)
                SubscriptionMessageStore.RemoveExpiredMessages(address, IsExpired, out expired);

            if (expired.Count == 0) continue;

            logger.LogDebug(nameof(ServiceBusMessageExpiryScheduler), nameof(ScanAndExpire),
                "{0} expired message(s) on '{1}' (deadLetter={2}).", expired.Count, address, deadLetter);

            if (!deadLetter) continue;

            var dlqAddress = $"{address}/$deadletterqueue";
            lock (OutgoingLinkEndpoint.DeliveryLock)
            {
                foreach (var msg in expired)
                {
                    msg.MessageAnnotations[new Symbol("x-opt-deadletter-reason")] = "TTLExpiredException";
                    msg.MessageAnnotations[new Symbol("x-opt-deadletter-error-description")] =
                        "Message TTL expired before the message was consumed.";
                    // The Azure.Messaging.ServiceBus SDK reads DeadLetterReason from
                    // ApplicationProperties, not from message annotations.
                    msg.ApplicationProperties ??= new ApplicationProperties();
                    msg.ApplicationProperties["DeadLetterReason"] = "TTLExpiredException";
                    msg.ApplicationProperties["DeadLetterErrorDescription"] =
                        "Message TTL expired before the message was consumed.";
                    SubscriptionMessageStore.Enqueue(dlqAddress, msg);
                }
            }
            OutgoingLinkEndpoint.NotifyMessageEnqueued();
        }
    }
}
