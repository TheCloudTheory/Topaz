using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.VirtualNetwork.Endpoints.VirtualNetworks;

internal sealed class CheckIpAddressAvailabilityEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly VirtualNetworkControlPlane _controlPlane =
        new(eventPipeline, new VirtualNetworkResourceProvider(logger), logger);

    public string ProviderNamespace => "Microsoft.Network";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/{virtualNetworkName}/checkIPAddressAvailability"
    ];

    public string[] Permissions => ["Microsoft.Network/virtualNetworks/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var virtualNetworkName = context.Request.Path.Value.ExtractValueFromPath(8);

        if (string.IsNullOrWhiteSpace(virtualNetworkName))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var ipAddress = context.Request.Query["ipAddress"].ToString();

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var operation = _controlPlane.CheckIpAddressAvailability(
            subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName, ipAddress);

        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.CreateJsonContentResponse(operation.Resource!);
    }
}
