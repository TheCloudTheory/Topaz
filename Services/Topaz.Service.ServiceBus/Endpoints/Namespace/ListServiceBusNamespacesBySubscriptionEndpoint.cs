using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ServiceBus.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.Namespace;

internal sealed class ListServiceBusNamespacesBySubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane =
        ServiceBusServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ServiceBus";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.ServiceBus/namespaces",
    ];

    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([
        GlobalSettings.DefaultResourceManagerPort, GlobalSettings.HttpsPort
    ], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));

        logger.LogDebug(nameof(ListServiceBusNamespacesBySubscriptionEndpoint), nameof(GetResponse),
            "Listing Service Bus namespaces for subscription {0}.", subscriptionIdentifier);

        var operation = _controlPlane.ListNamespacesBySubscription(subscriptionIdentifier);

        response.CreateJsonContentResponse(ListServiceBusNamespacesResponse.From(operation.Resource ?? []));
    }
}
