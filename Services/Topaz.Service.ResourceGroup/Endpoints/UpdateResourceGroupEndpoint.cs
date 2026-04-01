using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceGroup.Endpoints;

public class UpdateResourceGroupEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ResourceGroupControlPlane _controlPlane = ResourceGroupControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "PATCH /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}",
    ];

    public string[] Permissions => ["Microsoft.Resources/subscriptions/resourceGroups/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        using var reader = new StreamReader(context.Request.Body);

        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<UpdateResourceGroupRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var operation = _controlPlane.Update(subscriptionIdentifier, resourceGroupIdentifier, request);

        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.CreateJsonContentResponse(operation.Resource!);
    }
}
