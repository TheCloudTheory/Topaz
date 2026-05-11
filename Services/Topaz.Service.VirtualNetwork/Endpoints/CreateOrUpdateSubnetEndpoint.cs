using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.VirtualNetwork.Models.Requests;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.VirtualNetwork.Endpoints;

internal sealed class CreateOrUpdateSubnetEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubnetControlPlane _controlPlane = SubnetControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Network";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/{virtualNetworkName}/subnets/{subnetName}"
    ];

    public string[] Permissions => ["Microsoft.Network/virtualNetworks/subnets/write"];

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

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateSubnetRequest>(content, GlobalSettings.JsonOptions) ?? new CreateOrUpdateSubnetRequest();

        var operation = _controlPlane.CreateOrUpdate(
            subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName, subnetName, request);

        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var statusCode = operation.Result == OperationResult.Created ? HttpStatusCode.Created : HttpStatusCode.OK;
        response.CreateJsonContentResponse(operation.Resource!, statusCode);
    }
}
