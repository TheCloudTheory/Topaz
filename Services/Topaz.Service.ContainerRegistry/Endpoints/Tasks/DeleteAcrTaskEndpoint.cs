using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Tasks;

internal sealed class DeleteAcrTaskEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ContainerRegistryControlPlane _controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ContainerRegistry";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}/tasks/{taskName}"
    ];

    public string[] Permissions => ["Microsoft.ContainerRegistry/registries/tasks/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
        var registryName = path.ExtractValueFromPath(8)!;
        var taskName = path.ExtractValueFromPath(10)!;

        var operation = _controlPlane.DeleteTask(
            subscriptionIdentifier, resourceGroupIdentifier, registryName, taskName);

        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NoContent;
            response.Content = new StringContent(string.Empty);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(string.Empty);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
