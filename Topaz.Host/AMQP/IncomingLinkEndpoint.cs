using System.Text;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Types;
using Topaz.Host.AMQP.Filtering;
using Topaz.Service.ServiceBus.Filtering;
using Topaz.Service.ServiceBus.Models;
using Topaz.Shared;

namespace Topaz.Host.AMQP;

public class IncomingLinkEndpoint(string targetAddress, ITopazLogger logger, ServiceBusRuleLoader? ruleLoader = null) : LinkEndpoint
{
    private const uint BatchFormat = 0x80013700;

    public override void OnMessage(MessageContext messageContext)
    {
        logger.LogDebug(nameof(IncomingLinkEndpoint), nameof(OnMessage),
            $"Executing {nameof(IncomingLinkEndpoint)}.{nameof(OnMessage)}: Starting to process a message.");

        if (messageContext.Message.Body != null)
        {
            var body = messageContext.Message.Body as byte[] ?? [];
            var data = Encoding.UTF8.GetString(body);

            logger.LogDebug(nameof(IncomingLinkEndpoint), nameof(OnMessage), $"Processing message: {data}");
        }

        // Management endpoints use addresses like "sbqueue/$management".
        // Adding their request messages to the shared queue would route them to queue
        // consumers instead of the management response link. Skip them here.
        // Note: regular queue addresses are plain names like "py-queue-test" (no leading slash),
        // so $management is the correct discriminator — NOT a leading "/".
        if (targetAddress.Contains("$management", StringComparison.OrdinalIgnoreCase))
        {
            messageContext.Complete();
            return;
        }

        // TODO: Add support for messages sent as a batch
        if (messageContext.Message.Format == BatchFormat)
        {
            // The Event Hub producer SDK sends events as a batch AMQP message (Format=0x80013700).
            // The body is a single Data section containing concatenated AMQP-encoded individual
            // event messages. The real Event Hub broker unwraps these and delivers individual events
            // to consumers. Topaz must do the same: decode the inner messages and store them
            // individually so that consumers receive standard (non-batch) AMQP messages.
            messageContext.Complete();
            var batchBytes = messageContext.Message.Body as byte[];
            if (batchBytes != null && batchBytes.Length > 0)
            {
                var buf = new ByteBuffer(batchBytes, 0, batchBytes.Length, batchBytes.Length);
                while (buf.Length > 0)
                {
                    var inner = Message.Decode(buf);
                    PrepareMessageForDelivery(inner);
                    lock (OutgoingLinkEndpoint.DeliveryLock)
                    {
                        EnqueueMessage(inner);
                    }
                }
                OutgoingLinkEndpoint.NotifyMessageEnqueued();
            }
            return;
        }

        // Add message annotations which are used by Event Hub SDK for some of the internal operations
        PrepareMessageForDelivery(messageContext.Message);

        lock (OutgoingLinkEndpoint.DeliveryLock)
        {
            EnqueueMessage(messageContext.Message);
        }

        // Complete (send DISPOSITION to producer) BEFORE NotifyMessageEnqueued.
        // NotifyMessageEnqueued → SendMessageWithGuidTag sets delivery.Message on the new
        // outgoing Delivery object.  AMQPNetLite's Delivery.set_Message back-patches
        // message.Delivery to point at the outgoing delivery (link = consumer link).
        // If Complete() runs after that, ListenerLink.DisposeMessage finds
        // delivery.Link != this (the incoming producer link) and returns early without
        // sending any DISPOSITION frame.  The producer then waits 60 s for a DISPOSITION
        // that never arrives, releases the delivery, and retries — causing one publish
        // per minute instead of one per second.
        // Calling Complete() here, while message.Delivery still references the original
        // incoming delivery, ensures the DISPOSITION(accepted) is sent to the producer
        // immediately, matching real Azure Service Bus broker behaviour.
        messageContext.Complete();

        OutgoingLinkEndpoint.NotifyMessageEnqueued();

        logger.LogDebug(nameof(IncomingLinkEndpoint), nameof(OnMessage),
            $"Executing {nameof(IncomingLinkEndpoint)}.{nameof(OnMessage)}: Finished processing a message.");
    }

    /// <summary>
    /// Routes a prepared message to the appropriate entity queue(s).  For topic addresses
    /// the message is fanned out to all matching subscription queues; for queue addresses
    /// it is enqueued directly.
    ///
    /// Must be called while holding <see cref="OutgoingLinkEndpoint.DeliveryLock"/>.
    /// </summary>
    private void EnqueueMessage(Message message)
    {
        // AMQP target addresses arrive with a leading '/' (e.g. "/filter-topic").
        // Strip it so the name matches the filesystem directory name.
        var normalizedAddress = targetAddress.TrimStart('/');
        var isKnown = ruleLoader != null && ruleLoader.IsKnownTopic(normalizedAddress);
        logger.LogDebug(nameof(IncomingLinkEndpoint), nameof(EnqueueMessage),
            "address='{0}' isKnownTopic={1}", normalizedAddress, isKnown);
        if (isKnown)
        {
            FanOutToSubscriptions(normalizedAddress, message);
        }
        else
        {
            SubscriptionMessageStore.Enqueue(normalizedAddress, message);
        }
    }

    /// <summary>
    /// Fan-out: for each persisted subscription under this topic, evaluate the subscription's
    /// rules and — if the message matches — enqueue an independent clone into the
    /// subscription's per-entity queue.
    /// </summary>
    private void FanOutToSubscriptions(string topicName, Message original)
    {
        var subscriptionNames = ruleLoader!.GetSubscriptionNames(topicName);

        if (subscriptionNames.Length == 0)
        {
            logger.LogDebug(nameof(IncomingLinkEndpoint), nameof(FanOutToSubscriptions),
                "Topic '{0}' has no persisted subscriptions; message dropped.", targetAddress);
            return;
        }

        foreach (var subscriptionName in subscriptionNames)
        {
            // Subscription AMQP address: "{topic}/Subscriptions/{sub}"
            var subscriptionAddress = $"{topicName}/Subscriptions/{subscriptionName}";
            logger.LogDebug(nameof(IncomingLinkEndpoint), nameof(FanOutToSubscriptions),
                "topic='{0}' enqueue to subscription address='{1}'", topicName, subscriptionAddress);

            // Ensure the per-entity queue slot exists so messages accumulate even when
            // no consumer has attached yet (late-subscribe / durable-subscription semantics).
            SubscriptionMessageStore.EnsureQueue(subscriptionAddress);

            ServiceBusRuleResourceProperties[] rules;
            rules = ruleLoader.LoadRules(topicName, subscriptionName);

            if (!TopicSubscriptionRuleEvaluator.MatchesAny(original, rules))
            {
                logger.LogDebug(nameof(IncomingLinkEndpoint), nameof(FanOutToSubscriptions),
                    "Message did not match any rule for subscription '{0}'; skipping.", subscriptionName);
                continue;
            }

            // Clone the message so SqlRuleAction mutations on one subscription's copy
            // do not affect other subscriptions.
            var clone = CloneMessage(original);

            // Apply the first matching rule's action (if any).
            var matchingRule = Array.Find(rules, r => TopicSubscriptionRuleEvaluator.Matches(original, r));
            if (matchingRule?.Action != null)
                SqlRuleActionApplicator.Apply(matchingRule.Action, clone);

            SubscriptionMessageStore.Enqueue(subscriptionAddress, clone);

            logger.LogDebug(nameof(IncomingLinkEndpoint), nameof(FanOutToSubscriptions),
                "Enqueued message copy to subscription '{0}'.", subscriptionAddress);
        }
    }

    /// <summary>
    /// Produces an independent deep copy of an AMQP message by encoding then decoding it.
    /// </summary>
    private static Message CloneMessage(Message source)
    {
        var bytes = source.Encode();
        return Message.Decode(new ByteBuffer(bytes.Buffer, bytes.Offset, bytes.Length, bytes.Length));
    }

    /// <summary>
    /// Applies standard delivery preparation to a message before it is added to the
    /// outgoing queue: resets MessageAnnotations, assigns a fallback MessageId, and
    /// (on platforms where AMQPNetLite uses a ByteBuffer-backed Data section) eagerly
    /// copies the body bytes so the message is independent of the receive buffer lifetime.
    /// </summary>
    private static void PrepareMessageForDelivery(Message message)
    {
        // Reset/create MessageAnnotations — the Event Hub SDK reads system properties from here.
        message.MessageAnnotations = new MessageAnnotations();

        // Ensure every message has a MessageId. The Node.js @azure/service-bus SDK requires
        // messageId to track lock renewal; without it, the receiver throws
        // "Failed to stop auto lock renewal - no message ID".
        message.Properties ??= new Properties();
        if (message.Properties.MessageId == null)
            message.Properties.MessageId = Guid.NewGuid().ToString();
    }

    public override void OnFlow(FlowContext flowContext)
    {
        logger.LogDebug(nameof(IncomingLinkEndpoint), nameof(OnMessage),
            $"Executing {nameof(IncomingLinkEndpoint)}.{nameof(OnFlow)}: There will be a maximum of {flowContext.Messages} to process.");
    }

    public override void OnDisposition(DispositionContext dispositionContext)
    {
    }
}