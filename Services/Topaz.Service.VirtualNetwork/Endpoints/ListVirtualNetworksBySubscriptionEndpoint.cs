using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.VirtualNetwork.Endpoints;

internal sealed class ListVirtualNetworksBySubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly VirtualNetworkControlPlane _controlPlane = VirtualNetworkControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Network";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Network/virtualNetworks"
    ];

    public string[] Permissions => ["Microsoft.Network/virtualNetworks/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListVirtualNetworksBySubscriptionEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value!.ExtractValueFromPath(2));

        var vnets = _controlPlane.ListBySubscription(subscriptionIdentifier);
        if (vnets.Result != OperationResult.Success || vnets.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = new ListSubscriptionResourcesResponse
        {
            Value = vnets.Resource.Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From!).ToArray()
        };

        response.CreateJsonContentResponse(result);
    }
}
