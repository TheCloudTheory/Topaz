using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.EventHub.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.EventHub.Endpoints;

internal sealed class ListEventHubsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly EventHubServiceControlPlane _controlPlane = EventHubServiceControlPlane.New(logger);

    public string? ProviderNamespace => "Microsoft.EventHub";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventHub/namespaces/{namespaceName}/eventhubs",
    ];

    public string[] Permissions => ["Microsoft.EventHub/namespaces/eventhubs/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var namespaceIdentifier = EventHubNamespaceIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(8));

        var operation = _controlPlane.ListEventHubs(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);

        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var listResponse = new ListEventHubsResponse { Value = operation.Resource ?? [] };
        response.CreateJsonContentResponse(listResponse);
    }

    private sealed class ListEventHubsResponse
    {
        public EventHubResource[] Value { get; init; } = [];

        public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
