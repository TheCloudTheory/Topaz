using System.Collections.Concurrent;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Types;

namespace Topaz.Host.AMQP;

/// <summary>
/// Server-side sender for queue-specific $management response links.
/// The actual responses are sent by <see cref="QueueManagementRequestEndpoint"/>.
/// </summary>
internal sealed class QueueManagementResponseEndpoint : LinkEndpoint
{
    public override void OnMessage(MessageContext messageContext) { }
    public override void OnFlow(FlowContext flowContext) { }
    public override void OnDisposition(DispositionContext dispositionContext) { }
}

/// <summary>
/// Receives AMQP management requests sent to a queue-specific $management address
/// (e.g. "sbqueue/$management") — such as com.microsoft:complete, com.microsoft:abandon,
/// com.microsoft:renew-lock, com.microsoft:dead-letter — and sends 200 OK responses on the
/// paired response link so that the Azure SDK's RequestResponseAmqpLink does not time out.
/// </summary>
internal sealed class QueueManagementRequestEndpoint(
    ConcurrentDictionary<Session, ListenerLink> responseLinks) : LinkEndpoint
{
    public override void OnMessage(MessageContext messageContext)
    {
        // Settle the incoming delivery immediately.
        messageContext.Complete();

        if (!responseLinks.TryGetValue(messageContext.Link.Session, out var responseLink))
            return;

        var operation = messageContext.Message.ApplicationProperties?["operation"] as string;

        ApplicationProperties responseProperties;
        Message reply;

        if (operation == "com.microsoft:renew-lock")
        {
            responseProperties = new ApplicationProperties
            {
                Map =
                {
                    ["status-code"] = 200,
                    ["status-description"] = "OK"
                }
            };

            var renewBody = new Map
            {
                ["expiration"] = new[] { DateTime.UtcNow.AddMinutes(5) }
            };

            reply = new Message(renewBody) { ApplicationProperties = responseProperties };
        }
        else
        {
            // com.microsoft:complete, com.microsoft:abandon, com.microsoft:dead-letter, etc.
            responseProperties = new ApplicationProperties
            {
                Map =
                {
                    ["status-code"] = 200,
                    ["status-description"] = "OK"
                }
            };

            reply = new Message { ApplicationProperties = responseProperties };
        }

        reply.Properties = new Properties();

        // Mirror the request's message-id as the response's correlation-id so the SDK
        // can match it back to the pending RequestResponseAmqpLink.EndRequest call.
        var msgId = messageContext.Message.Properties?.GetMessageId();
        if (msgId != null)
            reply.Properties.SetCorrelationId(msgId);

        responseLink.SendMessage(reply);
    }

    public override void OnFlow(FlowContext flowContext) { }
    public override void OnDisposition(DispositionContext dispositionContext) { }
}
