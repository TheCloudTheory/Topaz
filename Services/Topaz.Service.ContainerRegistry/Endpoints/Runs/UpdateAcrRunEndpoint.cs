using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Runs;

internal sealed class UpdateAcrRunEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ContainerRegistryControlPlane _controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ContainerRegistry";

    public string[] Endpoints =>
    [
        "PATCH /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}/runs/{runId}"
    ];

    public string[] Permissions => ["Microsoft.ContainerRegistry/registries/runs/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
        var registryName = path.ExtractValueFromPath(8)!;
        var runId = path.ExtractValueFromPath(10)!;

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<UpdateAcrRunRequest>(content, GlobalSettings.JsonOptions)
                      ?? new UpdateAcrRunRequest();

        var operation = _controlPlane.UpdateRun(
            subscriptionIdentifier, resourceGroupIdentifier, registryName, runId, request);

        if (operation.Result == OperationResult.NotFound)
        {
            response.CreateErrorResponse(operation.Code!, operation.Reason!);
            return;
        }

        response.CreateJsonContentResponse(operation.Resource!);
    }
}
