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
        var id = Message.Decode(requestContext.Message.Encode()).Properties.MessageId;
        var p = new ApplicationProperties
        {
            Map =
            {
                ["status-code"] = 202,
                ["status-description"] = "Accepted"
            }
        };
        var reply = new Message() { Properties = new Properties { CorrelationId = id }, ApplicationProperties = p };

        requestContext.Complete(reply);
    }

    public int Credit => 10;
}