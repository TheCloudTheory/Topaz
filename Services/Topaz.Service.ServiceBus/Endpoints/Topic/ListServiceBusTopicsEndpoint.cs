using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ServiceBus.Models.Responses.Topic;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.Topic;

internal sealed class ListServiceBusTopicsEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane =
        ServiceBusServiceControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/topics",
    ];

    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/topics/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([
        GlobalSettings.DefaultResourceManagerPort, GlobalSettings.HttpsPort
    ], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var serviceBusNamespaceIdentifier =
            ServiceBusNamespaceIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(8));

        var operation = _controlPlane.ListTopics(subscriptionIdentifier, resourceGroupIdentifier,
            serviceBusNamespaceIdentifier);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.CreateErrorResponse(operation.Code!, operation.Reason!, operation.Result);
            return;
        }

        response.CreateJsonContentResponse(ListServiceBusTopicsResponse.From(operation.Resource));
    }
}