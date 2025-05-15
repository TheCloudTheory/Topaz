using Amqp.Listener;

namespace Topaz.Host.AMQP;

internal sealed class LinkProcessor : ILinkProcessor
{
    public void Process(AttachContext attachContext)
    {
        // TODO: MaxMessageSize should be based on the SKU of Azure Event Hub

        // Setting the MaxMessageSize property is required for Azure SDK
        // as the default value (long.MaxValue) overflows in the SDK giving -1
        // as the max size. This break communication as SDK thinks the maximum
        // size of a message or a batch is less than 0.
        attachContext.Attach.MaxMessageSize = 262144;
        attachContext.Complete(new IncomingLinkEndpoint(), 300);
    }
}