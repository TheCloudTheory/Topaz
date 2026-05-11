using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.VirtualNetwork.Endpoints;

internal sealed class DeleteSubnetEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubnetControlPlane _controlPlane = SubnetControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Network";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/{virtualNetworkName}/subnets/{subnetName}"
    ];

    public string[] Permissions => ["Microsoft.Network/virtualNetworks/subnets/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var virtualNetworkName = context.Request.Path.Value.ExtractValueFromPath(8);
        var subnetName = context.Request.Path.Value.ExtractValueFromPath(10);

        if (string.IsNullOrWhiteSpace(virtualNetworkName) || string.IsNullOrWhiteSpace(subnetName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var operation = _controlPlane.Delete(
            subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName, subnetName);

        response.StatusCode = operation.Result == OperationResult.NotFound
            ? HttpStatusCode.NotFound
            : HttpStatusCode.OK;

        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
    }
}
