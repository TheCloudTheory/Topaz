using System.Reflection;
using Amqp;
using Amqp.Listener;
using Amqp.Types;
using Topaz.Shared;

namespace Topaz.Host.AMQP;

public class OutgoingLinkEndpoint(ITopazLogger logger) : LinkEndpoint
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

    public override void OnFlow(FlowContext flowContext)
    {
        logger.LogDebug(nameof(OutgoingLinkEndpoint), nameof(OnFlow), "There will be a maximum of {0} to process with {1} messages available.", flowContext.Messages, IncomingLinkEndpoint.Messages.Count);
        if (flowContext.Link.Role) return;

        var messagesToSend = IncomingLinkEndpoint.Messages.Take(flowContext.Messages).ToList();
        foreach (var message in messagesToSend)
        {
            message.MessageAnnotations[new Symbol("x-opt-offset")] = IncomingLinkEndpoint.Messages.IndexOf(message).ToString();
            message.MessageAnnotations[new Symbol("x-opt-locked-until")] = DateTime.UtcNow.AddMinutes(5);

            SendMessageWithGuidTag(flowContext.Link, message);
            IncomingLinkEndpoint.Messages.Remove(message);
        }

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
        DeliverySettledProperty.SetValue(delivery, link.SettleOnSend);
        SessionSendDeliveryMethod.Invoke(link.Session, [delivery]);
    }
}