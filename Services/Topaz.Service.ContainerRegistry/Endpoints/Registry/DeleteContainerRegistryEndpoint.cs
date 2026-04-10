using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Registry;

internal sealed class DeleteContainerRegistryEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ContainerRegistryControlPlane _controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}"
    ];

    public string[] Permissions => ["Microsoft.ContainerRegistry/registries/delete"];

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
            response.Content = new StringContent(string.Empty);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return;
        }

        var operation = _controlPlane.Delete(
            subscriptionIdentifier,
            ResourceGroupIdentifier.From(resourceGroupName),
            registryName);

        response.StatusCode = operation.Result == OperationResult.NotFound
            ? HttpStatusCode.NoContent
            : HttpStatusCode.OK;

        if (operation.Resource != null)
            response.Content = new StringContent(operation.Resource.ToString());
        else
            response.Content = new StringContent(string.Empty);

        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
