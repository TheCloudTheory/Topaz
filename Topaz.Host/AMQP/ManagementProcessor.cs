using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Types;
using RequestContext = Amqp.Listener.RequestContext;

namespace Topaz.Host.AMQP;

/// <summary>
/// Processes initial request from Event Hub / Service Bus SDK asking for the service configuration.
/// </summary>
internal sealed class ManagementProcessor : IRequestProcessor
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

        var body = new Map
        {
            [ResponseMap.GeoReplicationFactor] = 1,
            [ResponseMap.Name] = "topaz_host",
            [ResponseMap.CreatedAt] = DateTime.UtcNow,
            [ResponseMap.PartitionIdentifiers] = new[] { Guid.NewGuid().ToString() }
        };

        var reply = new Message(body) { ApplicationProperties = p, };

        requestContext.Complete(reply);
    }

    public int Credit => 10;
}