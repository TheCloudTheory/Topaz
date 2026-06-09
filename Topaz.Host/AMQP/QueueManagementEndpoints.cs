using System.Collections.Concurrent;
using System.Text;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Types;
using Topaz.Shared;

namespace Topaz.Host.AMQP;

/// <summary>
/// Server-side sender for queue-specific $management response links.
/// The actual responses are sent by <see cref="QueueManagementRequestEndpoint"/>.
/// </summary>
internal sealed class QueueManagementResponseEndpoint : LinkEndpoint
{
    public override void OnMessage(MessageContext messageContext)
    {
    }

    public override void OnFlow(FlowContext flowContext)
    {
    }

    public override void OnDisposition(DispositionContext dispositionContext)
    {
    }
}

/// <summary>
/// Receives AMQP management requests sent to a queue-specific $management address
/// (e.g. "sbqueue/$management") — such as com.microsoft:complete, com.microsoft:abandon,
/// com.microsoft:renew-lock, com.microsoft:dead-letter — and sends 200 OK responses on the
/// paired response link so that the Azure SDK's RequestResponseAmqpLink does not time out.
/// </summary>
internal sealed class QueueManagementRequestEndpoint(
    ConcurrentDictionary<Session, ListenerLink> responseLinks,
    ITopazLogger logger) : LinkEndpoint
{
    public override void OnMessage(MessageContext messageContext)
    {
        var operation = messageContext.Message.ApplicationProperties?["operation"]?.ToString() ?? "<missing>";
        const int statusCode = 200;
        var requestBody = DescribeBody(messageContext.Message.Body);

        logger.LogInformation(
            $"[{nameof(QueueManagementRequestEndpoint)}.{nameof(OnMessage)}] Received queue management request: operation='{operation}', session='{messageContext.Link.Session}', hasResponseLink='{responseLinks.ContainsKey(messageContext.Link.Session)}'.");
        logger.LogInformation(
            $"[{nameof(QueueManagementRequestEndpoint)}.{nameof(OnMessage)}] Queue management request body: {requestBody}");

        // Settle the incoming delivery immediately.
        messageContext.Complete();

        if (!responseLinks.TryGetValue(messageContext.Link.Session, out var responseLink))
        {
            logger.LogError(nameof(QueueManagementRequestEndpoint), nameof(OnMessage),
                $"No queue management response link registered for session '{messageContext.Link.Session}'.");
            return;
        }

        Message reply;

        var responseProperties = new ApplicationProperties
        {
            Map =
            {
                ["statusCode"] = 200,
                ["statusDescription"] = "OK"
            }
        };

        if (operation == "com.microsoft:renew-lock")
        {
            var renewBody = new Map
            {
                ["expiration"] = new[] { DateTime.UtcNow.AddMinutes(5) }
            };

            reply = new Message(renewBody) { ApplicationProperties = responseProperties };
        }
        else
        {
            // com.microsoft:complete, com.microsoft:abandon, com.microsoft:dead-letter, etc.

            // Management replies are expected to be real AMQP management responses,
            // not a header-only message. Use an empty map body rather than a body-less
            // message, so the client-side response parser does not treat it as malformed.
            reply = new Message(new Map()) { ApplicationProperties = responseProperties };
        }

        reply.Properties = new Properties();

        // Mirror the request's message-id as the response's correlation-id so the SDK
        // can match it back to the pending RequestResponseAmqpLink.EndRequest call.
        var msgId = messageContext.Message.Properties?.GetMessageId();
        if (msgId != null)
            reply.Properties.SetCorrelationId(msgId);

        logger.LogInformation(
            $"[{nameof(QueueManagementRequestEndpoint)}.{nameof(OnMessage)}] Sending queue management response: operation='{operation}', statusCode='{statusCode}', correlationId='{msgId ?? "<missing>"}'.");

        responseLink.SendMessage(reply);
    }

    public override void OnFlow(FlowContext flowContext)
    {
    }

    public override void OnDisposition(DispositionContext dispositionContext)
    {
    }

    private static string DescribeBody(object? body)
    {
        switch (body)
        {
            case null:
                return "<null>";
            case Map map:
            {
                var builder = new StringBuilder();
                builder.Append('{');
                var first = true;
                foreach (var key in map.Keys)
                {
                    if (!first)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(key);
                    builder.Append('=');
                    builder.Append(map[key]);
                    first = false;
                }

                builder.Append('}');
                return builder.ToString();
            }
            default:
                return body.ToString() ?? body.GetType().FullName ?? "<unknown>";
        }
    }
}