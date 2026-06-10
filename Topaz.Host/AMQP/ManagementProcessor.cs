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
                    ["status-description"] = "OK",
                    ["statusCode"] = 200,
                    ["statusDescription"] = "OK"
                }
            };

            var renewBody = new Map
            {
                ["expiration"] = new[] { DateTime.UtcNow.AddMinutes(5) }
            };

            var renewResponse = new Message(renewBody) { ApplicationProperties = renewProperties };
            CompleteWithCorrelation(requestContext, renewResponse);
            return;
        }

        if (operation == "com.microsoft:partition" ||
            (operation == "READ" && requestContext.Message.ApplicationProperties?["type"]?.ToString() == "com.microsoft:partition"))
        {
            // GetPartitionPropertiesAsync from the Azure SDK requests partition-specific metadata.
            // The message body contains a Map with the partition ID.
            var bodyMap = requestContext.Message.Body as Map;
            var partitionId = bodyMap?["partition"] as string ?? "0";

            var partitionProperties = new ApplicationProperties
            {
                Map =
                {
                    ["status-code"] = 200,
                    ["status-description"] = "OK",
                    ["statusCode"] = 200,
                    ["statusDescription"] = "OK"
                }
            };

            var eventHubName = requestContext.Message.ApplicationProperties?["name"]?.ToString() ?? "topaz_host";
            var now = DateTime.UtcNow;

            var partitionBody = new Map
            {
                [ResponseMap.Name] = eventHubName,
                [ResponseMap.PartitionIdentifier] = partitionId,
                [ResponseMap.PartitionBeginSequenceNumber] = 0L,
                [ResponseMap.PartitionLastEnqueuedSequenceNumber] = 0L,
                [ResponseMap.PartitionLastEnqueuedOffset] = "0",
                [ResponseMap.PartitionLastEnqueuedTimeUtc] = now,
                [ResponseMap.PartitionRuntimeInfoRetrievalTimeUtc] = now,
                [ResponseMap.PartitionRuntimeInfoPartitionIsEmpty] = true
            };

            var partitionResponse = new Message(partitionBody) { ApplicationProperties = partitionProperties };
            CompleteWithCorrelation(requestContext, partitionResponse);
            return;
        }

        var p = new ApplicationProperties
        {
            Map =
            {
                ["status-code"] = 202,
                ["status-description"] = "Accepted",
                ["statusCode"] = 202,
                ["statusDescription"] = "Accepted"
            }
        };

        var body = new Map
        {
            [ResponseMap.GeoReplicationFactor] = 1,
            [ResponseMap.Name] = "topaz_host",
            [ResponseMap.CreatedAt] = DateTime.UtcNow,
            [ResponseMap.PartitionIdentifiers] = new[] { Guid.Empty.ToString() }
        };

        var response = new Message(body) { ApplicationProperties = p };
        CompleteWithCorrelation(requestContext, response);
    }

    private static void CompleteWithCorrelation(RequestContext requestContext, Message response)
    {
        response.Properties ??= new Properties();
        var requestMessageId = requestContext.Message.Properties?.GetMessageId();
        if (requestMessageId != null)
        {
            response.Properties.SetCorrelationId(requestMessageId);
        }

        requestContext.Complete(response);
    }

    public int Credit => 10;
}