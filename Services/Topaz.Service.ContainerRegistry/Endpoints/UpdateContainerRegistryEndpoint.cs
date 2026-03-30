using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints;

internal sealed class UpdateContainerRegistryEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ContainerRegistryControlPlane _controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "PATCH /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}"
    ];

    public string[] Permissions => ["Microsoft.ContainerRegistry/registries/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupName = path.ExtractValueFromPath(4);
        var registryName = path.ExtractValueFromPath(8);

        if (string.IsNullOrWhiteSpace(registryName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var getOperation = _controlPlane.Get(
            subscriptionIdentifier, ResourceGroupIdentifier.From(resourceGroupName), registryName);

        if (getOperation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateContainerRegistryRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var operation = _controlPlane.CreateOrUpdate(
            subscriptionIdentifier,
            ResourceGroupIdentifier.From(resourceGroupName),
            registryName,
            request);

        if (operation.Result != OperationResult.Updated)
        {
            response.CreateErrorResponse(operation.Code!, operation.Reason!, HttpStatusCode.BadRequest);
            return;
        }

        response.CreateJsonContentResponse(operation.Resource!);
    }
}
