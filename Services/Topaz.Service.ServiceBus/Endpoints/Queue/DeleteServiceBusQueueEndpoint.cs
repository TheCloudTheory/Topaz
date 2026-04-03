using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.Queue;

internal sealed class DeleteServiceBusQueueEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane =
        ServiceBusServiceControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/queues/{queueName}",
    ];

    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/queues/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([
        GlobalSettings.DefaultResourceManagerPort, GlobalSettings.HttpsPort
    ], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var serviceBusNamespaceIdentifier = ServiceBusNamespaceIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(8));
        var queueName = context.Request.Path.Value.ExtractValueFromPath(10);

        var operation = _controlPlane.DeleteQueue(subscriptionIdentifier, resourceGroupIdentifier, serviceBusNamespaceIdentifier, queueName!);
        if (operation.Result == OperationResult.NotFound)
        {
            response.CreateErrorResponse(operation.Code!, operation.Reason!, operation.Result);
            return;
        }

        response.StatusCode = HttpStatusCode.NoContent;
    }
}