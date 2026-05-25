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
        var operation = requestContext.Message.ApplicationProperties?["operation"] as string;

        if (operation == "com.microsoft:renew-lock")
        {
            var renewProperties = new ApplicationProperties
            {
                Map =
                {
                    ["status-code"] = 200,
                    ["status-description"] = "OK"
                }
            };

            var renewBody = new Map
            {
                ["expiration"] = new[] { DateTime.UtcNow.AddMinutes(5) }
            };

            requestContext.Complete(new Message(renewBody) { ApplicationProperties = renewProperties });
            return;
        }

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
            [ResponseMap.PartitionIdentifiers] = new[] { Guid.Empty.ToString() }
        };

        requestContext.Complete(new Message(body) { ApplicationProperties = p });
    }

    public int Credit => 10;
}