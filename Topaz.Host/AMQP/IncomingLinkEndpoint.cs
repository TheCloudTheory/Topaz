using Amqp.Listener;

namespace Topaz.Host.AMQP;

public class IncomingLinkEndpoint : LinkEndpoint
{
    public override void OnMessage(MessageContext messageContext)
    {
        // this can also be done when an async operation, if required, is done
        messageContext.Complete();
    }

    public override void OnFlow(FlowContext flowContext)
    {
    }

    public override void OnDisposition(DispositionContext dispositionContext)
    {
    }
}