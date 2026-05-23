using System.Collections.Concurrent;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;

namespace Topaz.Host.AMQP;

/// <summary>
/// Endpoint for the server-side sender link in CBS ($cbs, client's ReceiverLink).
/// CBS responses are sent via <see cref="CbsRequestEndpoint"/> using the ListenerLink
/// stored in the session registry; this class only satisfies the LinkEndpoint contract.
/// </summary>
internal sealed class CbsResponseEndpoint : LinkEndpoint
{
    public override void OnMessage(MessageContext messageContext) { }
    public override void OnFlow(FlowContext flowContext) { }
    public override void OnDisposition(DispositionContext dispositionContext) { }
}

/// <summary>
/// Receives CBS (Claims-Based Security) put-token requests and sends 200 OK responses
/// back on the session's registered CBS response link.
/// Does not require <c>reply_to</c> to be set on the request message.
/// </summary>
/// <remarks>
/// pyamqp's <c>CBSAuthenticator</c> creates a <c>ManagementLink</c> with
/// <c>status_code_field=b"status-code"</c>.  pyamqp's <c>_decode_binary_small</c>
/// decodes every AMQP variable-length type (str8 / sym8) as Python <c>bytes</c>,
/// so the AMQP string <c>"status-code"</c> arrives as <c>b"status-code"</c> which
/// matches the field key exactly.
/// </remarks>
internal sealed class CbsRequestEndpoint(
    ConcurrentDictionary<Session, ListenerLink> responseLinks) : LinkEndpoint
{
    public override void OnMessage(MessageContext messageContext)
    {
        // Settle the incoming delivery immediately as Accepted so pyamqp's
        // sender does not spin waiting for a disposition.
        messageContext.Complete();

        if (!responseLinks.TryGetValue(messageContext.Link.Session, out var responseLink))
            return;

        // Build a CBS response whose application-properties key matches the
        // status_code_field configured by CBSAuthenticator ("status-code").
        var reply = new Message
        {
            Properties = new Properties(),
            ApplicationProperties = new ApplicationProperties
            {
                Map =
                {
                    ["status-code"] = 200,
                    ["status-description"] = "OK",
                },
            },
        };

        // Mirror the request's message-id as the response's correlation-id.
        // GetMessageId / SetCorrelationId handle all AMQP ID types (Guid, string, etc.)
        // without the InvalidCastException that the string-typed accessors throw.
        var msgId = messageContext.Message.Properties?.GetMessageId();
        if (msgId != null)
        {
            reply.Properties.SetCorrelationId(msgId);
        }

        responseLink.SendMessage(reply);
    }

    public override void OnFlow(FlowContext flowContext) { }
    public override void OnDisposition(DispositionContext dispositionContext) { }
}
