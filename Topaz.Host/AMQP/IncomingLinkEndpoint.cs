using Amqp;
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
        for (var i = 0; i < flowContext.Messages; i++)
        {
            flowContext.Link.SendMessage(new Message("Hello from Topaz!"));
        }
    }

    public override void OnDisposition(DispositionContext dispositionContext)
    {
    }
}