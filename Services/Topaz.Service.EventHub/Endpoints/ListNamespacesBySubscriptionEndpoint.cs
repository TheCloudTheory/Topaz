using Microsoft.AspNetCore.Http;
using Topaz.Service.EventHub.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.EventHub.Endpoints;

internal sealed class ListNamespacesBySubscriptionEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly EventHubServiceControlPlane _controlPlane = EventHubServiceControlPlane.New(logger);

    public string? ProviderNamespace => "Microsoft.EventHub";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.EventHub/namespaces",
    ];

    public string[] Permissions => ["Microsoft.EventHub/namespaces/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort, GlobalSettings.HttpsPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));

        logger.LogDebug(nameof(ListNamespacesBySubscriptionEndpoint), nameof(GetResponse),
            "Listing namespaces for subscription {0}.", subscriptionIdentifier);

        var operation = _controlPlane.ListNamespacesBySubscription(subscriptionIdentifier);

        response.CreateJsonContentResponse(ListEventHubNamespacesResponse.From(operation.Resource ?? []));
    }
}
