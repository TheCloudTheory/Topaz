using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Tasks;

internal sealed class CreateOrUpdateAcrTaskEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ContainerRegistryControlPlane _controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ContainerRegistry";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}/tasks/{taskName}"
    ];

    public string[] Permissions => ["Microsoft.ContainerRegistry/registries/tasks/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
        var registryName = path.ExtractValueFromPath(8)!;
        var taskName = path.ExtractValueFromPath(10)!;

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateAcrTaskRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var operation = _controlPlane.CreateOrUpdateTask(
            subscriptionIdentifier, resourceGroupIdentifier, registryName, taskName, request);

        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated)
        {
            response.CreateErrorResponse(operation.Code!, operation.Reason!, HttpStatusCode.BadRequest);
            return;
        }

        var statusCode = operation.Result == OperationResult.Created
            ? HttpStatusCode.Created
            : HttpStatusCode.OK;
        response.CreateJsonContentResponse(operation.Resource!, statusCode);
    }
}
