using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceGroup.Endpoints;

public class CheckExistenceResourceGroupEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ResourceGroupControlPlane _controlPlane = ResourceGroupControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "HEAD /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}",
    ];

    public string[] Permissions => ["Microsoft.Resources/subscriptions/resourceGroups/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var operation = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);

        response.StatusCode = operation.Result == OperationResult.NotFound
            ? HttpStatusCode.NotFound
            : HttpStatusCode.NoContent;
    }
}
