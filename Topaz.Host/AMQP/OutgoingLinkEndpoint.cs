using Amqp.Listener;
using Amqp.Types;
using Topaz.Shared;

namespace Topaz.Host.AMQP;

public class OutgoingLinkEndpoint(ITopazLogger logger) : LinkEndpoint
{
    public override void OnFlow(FlowContext flowContext)
    {
        logger.LogDebug($"Executing {nameof(OutgoingLinkEndpoint)}.{nameof(OnFlow)}: There will be a maximum of {flowContext.Messages} to process with {IncomingLinkEndpoint.Messages.Count} messages available.");
        if (flowContext.Link.Role) return;
        
        var messagesToSend = IncomingLinkEndpoint.Messages.Take(flowContext.Messages);
        foreach (var message in messagesToSend)
        {
            message.MessageAnnotations[new Symbol("x-opt-offset")] = IncomingLinkEndpoint.Messages.IndexOf(message).ToString();
            
            flowContext.Link.SendMessage(message);
            IncomingLinkEndpoint.Messages.Remove(message);
        }

        if (flowContext.Link.IsDraining)
        {
            logger.LogDebug($"Executing {nameof(OutgoingLinkEndpoint)}.{nameof(OnFlow)}: Completing draining.");
            flowContext.Link.CompleteDrain();
            logger.LogDebug($"Executing {nameof(OutgoingLinkEndpoint)}.{nameof(OnFlow)}: Draining complete.");
        }
        
        logger.LogDebug($"Executing {nameof(OutgoingLinkEndpoint)}.{nameof(OnFlow)}: Finished processing messages.");
    }

    public override void OnDisposition(DispositionContext dispositionContext)
    {
    }
}