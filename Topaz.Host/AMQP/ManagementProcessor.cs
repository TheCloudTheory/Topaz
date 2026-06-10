using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Types;
using RequestContext = Amqp.Listener.RequestContext;
using Topaz.Shared;

namespace Topaz.Host.AMQP;

/// <summary>
/// Processes initial request from Event Hub / Service Bus SDK asking for the service configuration.
/// </summary>
internal sealed class ManagementProcessor(ITopazLogger logger) : IRequestProcessor
{
    public void Process(RequestContext requestContext)
    {
        var operation = requestContext.Message.ApplicationProperties?["operation"] as string;
        var operationType = requestContext.Message.ApplicationProperties?["type"] as string;
        var bodyType = requestContext.Message.Body?.GetType().Name ?? "null";
        var appPropsKeys = string.Join(", ", (requestContext.Message.ApplicationProperties?.Map ?? []).Keys);
        
        logger.LogDebug(nameof(ManagementProcessor), nameof(Process), 
            $"Management request: operation='{operation}', type='{operationType}', bodyType='{bodyType}', " +
            $"appProps=[{appPropsKeys}]");

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

            // Return BOTH snake_case (for .NET SDK) AND camelCase (for Node.js SDK) field names
            // Each SDK will use the variant it expects and ignore the other
            var partitionBody = new Map
            {
                // .NET SDK expects these (snake_case):
                [ResponseMap.Name] = eventHubName,
                [ResponseMap.PartitionIdentifier] = partitionId,
                [ResponseMap.PartitionBeginSequenceNumber] = 0L,
                [ResponseMap.PartitionLastEnqueuedSequenceNumber] = 0L,
                [ResponseMap.PartitionLastEnqueuedOffset] = "0",
                [ResponseMap.PartitionLastEnqueuedTimeUtc] = now,
                [ResponseMap.PartitionRuntimeInfoRetrievalTimeUtc] = now,
                [ResponseMap.PartitionRuntimeInfoPartitionIsEmpty] = true,
                
                // Node.js SDK expects these (camelCase):
                [ResponseMap.EventHubName] = eventHubName,
                [ResponseMap.PartitionIdCamelCase] = partitionId,
                [ResponseMap.BeginningSequenceNumber] = 0L,
                [ResponseMap.LastEnqueuedSequenceNumberCamelCase] = 0L,
                [ResponseMap.LastEnqueuedOffsetCamelCase] = "0",
                [ResponseMap.LastEnqueuedOnUtc] = now,
                [ResponseMap.RetrievalTimeUtc] = now,
                [ResponseMap.IsEmpty] = true
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
            [ResponseMap.PartitionIdentifiersSnakeCase] = new[] { Guid.Empty.ToString() },
            // Also include camelCase variants for Node.js SDK
            [ResponseMap.PartitionIdentifiersCamelCase] = new[] { Guid.Empty.ToString() }
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