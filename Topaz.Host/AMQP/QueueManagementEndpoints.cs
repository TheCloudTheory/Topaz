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
    ITopazLogger logger,
    Service.ServiceBus.Filtering.ServiceBusRuleLoader? ruleLoader = null,
    string entityAddress = "") : LinkEndpoint
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
        else if (operation is "com.microsoft:complete" or "com.microsoft:abandon" or "com.microsoft:dead-letter")
        {
            ProcessLockTokenOperation(operation, messageContext.Message.Body);
            reply = new Message(new Map()) { ApplicationProperties = responseProperties };
        }
        else if (operation == "com.microsoft:renew-session-lock")
        {
            var sessionId = ExtractSessionId(messageContext.Message.Body);
            DateTimeOffset expiry;
            lock (OutgoingLinkEndpoint.DeliveryLock)
            {
                expiry = SessionMessageStore.RenewSessionLock(entityAddress, sessionId ?? string.Empty);
            }
            var renewSessionBody = new Map
            {
                ["expiration"] = new[] { expiry.UtcDateTime }
            };
            reply = new Message(renewSessionBody) { ApplicationProperties = responseProperties };
        }
        else if (operation == "com.microsoft:get-session-state")
        {
            var sessionId = ExtractSessionId(messageContext.Message.Body);
            byte[]? state;
            lock (OutgoingLinkEndpoint.DeliveryLock)
            {
                state = SessionMessageStore.GetSessionState(entityAddress, sessionId ?? string.Empty);
            }
            var getStateBody = new Map
            {
                ["session-state"] = state
            };
            reply = new Message(getStateBody) { ApplicationProperties = responseProperties };
        }
        else if (operation == "com.microsoft:set-session-state")
        {
            var sessionId = ExtractSessionId(messageContext.Message.Body);
            byte[]? state = null;
            if (messageContext.Message.Body is Map setStateMap && setStateMap.ContainsKey("session-state"))
                state = setStateMap["session-state"] as byte[];
            lock (OutgoingLinkEndpoint.DeliveryLock)
            {
                SessionMessageStore.SetSessionState(entityAddress, sessionId ?? string.Empty, state);
            }
            reply = new Message(new Map()) { ApplicationProperties = responseProperties };
        }
        else
        {
            // Other operations (e.g. peek, schedule) — return empty OK response.
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

    private void ProcessLockTokenOperation(string operation, object? body)
    {
        if (body is not Map bodyMap)
            return;

        var lockTokens = ExtractLockTokens(bodyMap["lock-tokens"]);

        lock (OutgoingLinkEndpoint.DeliveryLock)
        {
            foreach (var lockToken in lockTokens)
            {
                switch (operation)
                {
                    case "com.microsoft:complete":
                        InFlightMessageStore.TryCompleteByLockToken(lockToken);
                        break;

                    case "com.microsoft:abandon":
                        InFlightMessageStore.HandleAbandonByLockToken(lockToken,
                            ruleLoader != null
                                ? ResolveMaxDeliveryCount(lockToken)
                                : 10);
                        break;

                    case "com.microsoft:dead-letter":
                        var reason = bodyMap.ContainsKey("deadletter-reason")
                            ? bodyMap["deadletter-reason"]?.ToString()
                            : null;
                        var description = bodyMap.ContainsKey("deadletter-error-description")
                            ? bodyMap["deadletter-error-description"]?.ToString()
                            : null;
                        InFlightMessageStore.HandleDeadLetterByLockToken(lockToken, reason, description);
                        break;
                }
            }
        }
    }

    private int ResolveMaxDeliveryCount(Guid lockToken) => 10;

    private static string? ExtractSessionId(object? body)
    {
        if (body is Map map && map.ContainsKey("session-id"))
            return map["session-id"]?.ToString();
        return null;
    }

    private static IEnumerable<Guid> ExtractLockTokens(object? value) => value switch
    {
        Guid[] guids              => guids,
        Guid single               => [single],
        object[] objs             => objs.OfType<Guid>(),
        IEnumerable<object> list  => list.OfType<Guid>(),
        _                         => []
    };

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