using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.EventHub.Endpoints;

public class DeleteEventHubEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly EventHubServiceControlPlane _controlPlane = new(new EventHubResourceProvider(logger), logger);

    public string? ProviderNamespace => "Microsoft.EventHub";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventHub/namespaces/{namespaceName}/eventhubs/{eventHubName}"
    ];

    public string[] Permissions => ["Microsoft.EventHub/namespaces/eventhubs/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var namespaceIdentifier = EventHubNamespaceIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(8));
        var eventHubName = context.Request.Path.Value.ExtractValueFromPath(10);

        logger.LogDebug(nameof(DeleteEventHubEndpoint), nameof(GetResponse),
            "Deleting event hub {0} from namespace {1}", eventHubName, namespaceIdentifier.Value);

        var operation = _controlPlane.Delete(subscriptionIdentifier, resourceGroupIdentifier, eventHubName!, namespaceIdentifier);

        response.StatusCode = operation.Result switch
        {
            OperationResult.NotFound => HttpStatusCode.NotFound,
            OperationResult.Deleted => HttpStatusCode.OK,
            _ => HttpStatusCode.InternalServerError
        };
    }
}
