using System.Text;
using Amqp;
using Amqp.Listener;
using Topaz.Shared;

namespace Topaz.Host.AMQP;

public class IncomingLinkEndpoint(ITopazLogger logger) : LinkEndpoint
{
    private const uint BatchFormat = 0x80013700;
    private static readonly List<Message> Messages = [];
    
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
        
        Messages.Add(messageContext.Message);
        messageContext.Complete();
        
        logger.LogDebug($"Executing {nameof(IncomingLinkEndpoint)}.{nameof(OnMessage)}: Finished processing a message.");
    }

    public override void OnFlow(FlowContext flowContext)
    {
        logger.LogDebug($"Executing {nameof(IncomingLinkEndpoint)}.{nameof(OnFlow)}: There will be a maximum of {flowContext.Messages} to process with {Messages.Count} messages available.");
        
        var messagesToSend = Messages.Take(flowContext.Messages);
        foreach (var message in messagesToSend)
        {
            flowContext.Link.SendMessage(message);
            Messages.Remove(message);
        }

        if (flowContext.Link.IsDraining)
        {
            logger.LogDebug($"Executing {nameof(IncomingLinkEndpoint)}.{nameof(OnFlow)}: Completing draining.");
            flowContext.Link.CompleteDrain();
            logger.LogDebug($"Executing {nameof(IncomingLinkEndpoint)}.{nameof(OnFlow)}: Draining complete.");
        }
        
        logger.LogDebug($"Executing {nameof(IncomingLinkEndpoint)}.{nameof(OnFlow)}: Finished processing messages.");
    }

    public override void OnDisposition(DispositionContext dispositionContext)
    {
    }
}