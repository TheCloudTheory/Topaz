using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ServiceBus.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.Namespace;

internal sealed class ListServiceBusNamespacesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane =
        ServiceBusServiceControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces",
    ];

    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([
        GlobalSettings.DefaultResourceManagerPort, GlobalSettings.HttpsPort
    ], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));

        var operation = _controlPlane.ListNamespaces(subscriptionIdentifier, resourceGroupIdentifier);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.CreateErrorResponse(operation.Code!, operation.Reason!, operation.Result);
            return;
        }

        response.CreateJsonContentResponse(ListServiceBusNamespacesResponse.From(operation.Resource));
    }
}