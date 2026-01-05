using System.Text;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Topaz.Shared;

namespace Topaz.Host.AMQP;

public class IncomingLinkEndpoint(ITopazLogger logger) : LinkEndpoint
{
    private const uint BatchFormat = 0x80013700;
    public static readonly List<Message> Messages = [];
    
    public override void OnMessage(MessageContext messageContext)
    {
        logger.LogDebug($"Executing {nameof(IncomingLinkEndpoint)}.{nameof(OnMessage)}: Starting to process a message.");
        
        if (messageContext.Message.Body != null)
        {
            var body = messageContext.Message.Body as byte[] ?? [];
            var data = Encoding.UTF8.GetString(body);
            
            logger.LogDebug($"Processing message: {data}");
        }

        // TODO: Add support for messages sent as a batch
        if (messageContext.Message.Format == BatchFormat)
        {
        }

        // Add message annotations which are used by Event Hub SDK for some of the internal operations
        messageContext.Message.MessageAnnotations = new MessageAnnotations();
        
        Messages.Add(messageContext.Message);
        messageContext.Complete();
        
        logger.LogDebug($"Executing {nameof(IncomingLinkEndpoint)}.{nameof(OnMessage)}: Finished processing a message.");
    }

    public override void OnFlow(FlowContext flowContext)
    {
        logger.LogDebug($"Executing {nameof(IncomingLinkEndpoint)}.{nameof(OnFlow)}: There will be a maximum of {flowContext.Messages} to process with {Messages.Count} messages available.");
    }

    public override void OnDisposition(DispositionContext dispositionContext)
    {
    }

    public override void OnLinkClosed(ListenerLink link, Error error)
    {
        base.OnLinkClosed(link, error);
    }
}