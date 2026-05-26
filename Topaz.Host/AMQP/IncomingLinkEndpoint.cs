using System.Text;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Topaz.Shared;

namespace Topaz.Host.AMQP;

public class IncomingLinkEndpoint(string targetAddress, ITopazLogger logger) : LinkEndpoint
{
    private const uint BatchFormat = 0x80013700;
    public static readonly List<Message> Messages = [];

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
        }

        // Add message annotations which are used by Event Hub SDK for some of the internal operations
        messageContext.Message.MessageAnnotations = new MessageAnnotations();

        lock (OutgoingLinkEndpoint.DeliveryLock)
        {
            Messages.Add(messageContext.Message);
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

    public override void OnFlow(FlowContext flowContext)
    {
        logger.LogDebug(nameof(IncomingLinkEndpoint), nameof(OnMessage),
            $"Executing {nameof(IncomingLinkEndpoint)}.{nameof(OnFlow)}: There will be a maximum of {flowContext.Messages} to process with {Messages.Count} messages available.");
    }

    public override void OnDisposition(DispositionContext dispositionContext)
    {
    }
}