using System.Reflection;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Types;
using Topaz.Shared;

namespace Topaz.Host.AMQP;

public class OutgoingLinkEndpoint(string sourceAddress, ITopazLogger logger) : LinkEndpoint
{
    // The Azure SDK (AmqpMessageConverter) reads LockTokenGuid from the AMQP Transfer delivery tag,
    // but only if it is exactly 16 bytes (GuidUtilities.TryParseGuidBytes requires length == 16).
    // AMQPNetLite's ListenerLink.SendMessage generates a 4-byte counter delivery tag, which the SDK
    // ignores, leaving LockTokenGuid as Guid.Empty and causing ThrowIfLockTokenIsEmpty to throw.
    // The fix bypasses the built-in SendMessage to supply a 16-byte GUID delivery tag via Session.SendDelivery.
    private static readonly Type DeliveryType =
        typeof(ListenerLink).Assembly.GetTypes().First(t => t.Name == "Delivery");

    private static readonly PropertyInfo DeliveryTagProperty =
        DeliveryType.GetProperty("Tag")!;

    private static readonly PropertyInfo DeliveryMessageProperty =
        DeliveryType.GetProperty("Message")!;

    private static readonly FieldInfo DeliveryBufferField =
        DeliveryType.GetField("Buffer")!;

    private static readonly FieldInfo DeliveryHandleField =
        DeliveryType.GetField("Handle")!;

    private static readonly FieldInfo DeliveryLinkField =
        DeliveryType.GetField("Link")!;

    private static readonly PropertyInfo DeliverySettledProperty =
        DeliveryType.GetProperty("Settled")!;

    private static readonly MethodInfo SessionSendDeliveryMethod =
        typeof(ListenerLink).Assembly.GetTypes().First(t => t.Name == "Session")
            .GetMethod("SendDelivery", BindingFlags.NonPublic | BindingFlags.Instance)!;

    // Protects _queueEndpoints, per-instance _pendingLink/_pendingCredit, and IncomingLinkEndpoint.Messages.
    internal static readonly object DeliveryLock = new();

    // Registry of active OutgoingLinkEndpoint instances that serve real queue/topic addresses
    // (source address starts with "/"). Management links (e.g. "sbqueue/$management") are
    // excluded so their FLOW frames cannot hijack the shared delivery state.
    private static readonly List<OutgoingLinkEndpoint> _queueEndpoints = [];

    // Per-instance state — was static, which caused FLOW from any receiver link (including
    // AMQP management response links) to overwrite the single shared _pendingLink and steal
    // all subsequent deliveries away from the real queue consumer.
    private ListenerLink? _pendingLink;
    private int _pendingCredit;

    // Called by IncomingLinkEndpoint after a message is added to the queue.
    internal static void NotifyMessageEnqueued()
    {
        DeliverMessages();
    }

    private static void DeliverMessages()
    {
        List<(ListenerLink Link, Message Message)> deliveries = [];

        lock (DeliveryLock)
        {
            foreach (var ep in _queueEndpoints)
            {
                while (ep._pendingCredit > 0 && IncomingLinkEndpoint.Messages.Count > 0 && ep._pendingLink != null)
                {
                    var message = IncomingLinkEndpoint.Messages[0];
                    message.Header ??= new Header();
                    // AMQPNetLite only writes a field into the encoded list when the backing store has been
                    // explicitly set via the property setter — it does NOT encode fields whose backing store
                    // is null even when the getter returns the default (0). Re-assigning DeliveryCount
                    // through the setter forces the value into the backing store so it appears as
                    // delivery-count=0 (0x43) in the binary. Microsoft.Azure.Amqp decodes that as
                    // Nullable<uint> with HasValue=true, which AmqpMessageToSBReceivedMessage requires in
                    // order to populate ServiceBusReceivedMessage.DeliveryCount without throwing.
                    message.Header.DeliveryCount = message.Header.DeliveryCount;
                    message.MessageAnnotations[new Symbol("x-opt-offset")] = IncomingLinkEndpoint.Messages.IndexOf(message).ToString();
                    message.MessageAnnotations[new Symbol("x-opt-locked-until")] = DateTime.UtcNow.AddMinutes(5);
                    IncomingLinkEndpoint.Messages.RemoveAt(0);
                    ep._pendingCredit--;
                    deliveries.Add((ep._pendingLink, message));
                }
            }
        }

        // Send outside the lock to avoid holding it during network I/O.
        foreach (var (link, message) in deliveries)
        {
            SendMessageWithGuidTag(link, message);
        }
    }

    public override void OnFlow(FlowContext flowContext)
    {
        logger.LogDebug(nameof(OutgoingLinkEndpoint), nameof(OnFlow), "There will be a maximum of {0} to process with {1} messages available.", flowContext.Messages, IncomingLinkEndpoint.Messages.Count);
        if (flowContext.Link.Role) return;

        lock (DeliveryLock)
        {
            _pendingLink = flowContext.Link;
            _pendingCredit = flowContext.Messages;

            // Register this endpoint in the queue-delivery registry on first FLOW,
            // but only for real queue/topic addresses (not management links).
            // AMQP management links (e.g. "sbqueue/$management") send FLOW with
            // link-credit:50 at startup. Before this fix those flows overwrote the
            // single shared static _pendingLink, redirecting all subsequent deliveries
            // to the management response link instead of the queue consumer.
            // Note: regular queue addresses are plain names like "py-queue-test" (no
            // leading slash), so $management is the correct discriminator — NOT "/".
            if (!sourceAddress.Contains("$management", StringComparison.OrdinalIgnoreCase) && !_queueEndpoints.Contains(this))
            {
                _queueEndpoints.Add(this);
                flowContext.Link.Closed += (_, _) =>
                {
                    lock (DeliveryLock) { _queueEndpoints.Remove(this); }
                };
            }
        }

        DeliverMessages();

        if (flowContext.Link.IsDraining)
        {
            logger.LogDebug(nameof(OutgoingLinkEndpoint), nameof(OnFlow), "Completing draining.");
            flowContext.Link.CompleteDrain();
            logger.LogDebug(nameof(OutgoingLinkEndpoint), nameof(OnFlow), "Draining complete.");
        }

        logger.LogDebug(nameof(OutgoingLinkEndpoint), nameof(OnFlow), "Finished processing messages.");
    }

    public override void OnDisposition(DispositionContext dispositionContext)
    {
        // The client (Azure SDK / MassTransit) sends a DISPOSITION frame after processing a message.
        // Because TRANSFER frames are sent pre-settled (Settled=true), the receiver never adds the
        // message to its unsettledMap and CompleteMessageAsync returns immediately without waiting for
        // a broker confirmation.  Complete() is still called here to acknowledge the disposition and
        // keep the AMQP session state clean.
        dispositionContext.Complete();
    }

    private static void SendMessageWithGuidTag(ListenerLink link, Message message)
    {
        var buffer = message.Encode();
        var delivery = Activator.CreateInstance(DeliveryType)!;
        DeliveryTagProperty.SetValue(delivery, Guid.NewGuid().ToByteArray());
        DeliveryMessageProperty.SetValue(delivery, message);
        DeliveryBufferField.SetValue(delivery, buffer);
        DeliveryHandleField.SetValue(delivery, link.Handle);
        DeliveryLinkField.SetValue(delivery, link);
        // Service Bus queue receivers operate in PeekLock mode and expect normal unsettled
        // transfers. Sender-settled deliveries consume the initial credit but do not appear to
        // trigger credit replenishment in the MassTransit/Azure Service Bus receive path, which
        // leaves the consumer stuck after the first message. Use unsettled transfers and let the
        // receiver settle explicitly.
        DeliverySettledProperty.SetValue(delivery, false);
        SessionSendDeliveryMethod.Invoke(link.Session, [delivery]);
    }
}