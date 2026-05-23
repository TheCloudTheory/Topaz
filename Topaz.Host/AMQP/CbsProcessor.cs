using Amqp;
using Amqp.Framing;
using Amqp.Listener;

namespace Topaz.Host.AMQP;

/// <summary>
/// Processes CBS (Claims-Based Security) requests by implementing the <see cref="IRequestProcessor"/> interface.
/// </summary>
public class CbsProcessor : IRequestProcessor
{
    public void Process(RequestContext requestContext)
    {
        var p = new ApplicationProperties
        {
            Map =
            {
                ["status-code"] = 202,
                ["status-description"] = "Accepted"
            }
        };

        var reply = new Message { ApplicationProperties = p };
        reply.Properties = new Properties();

        // Use GetMessageId() / SetCorrelationId() instead of the string-typed
        // MessageId / CorrelationId properties. The string-typed getters cast to
        // (string) internally, which throws InvalidCastException when the sender
        // (e.g. azure-servicebus pyamqp) sends the message-id as a UUID type
        // rather than a string type.
        var msgId = requestContext.Message.Properties?.GetMessageId();
        if (msgId != null)
        {
            reply.Properties.SetCorrelationId(msgId);
        }

        var responseLink = requestContext.ResponseLink;
        if (responseLink != null)
        {
            responseLink.SendMessage(reply);
            requestContext.Message.Dispose();
        }
    }

    public int Credit => 10;
}