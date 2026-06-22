using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Runs;

internal sealed class ScheduleAcrRunEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ContainerRegistryControlPlane _controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ContainerRegistry";

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}/scheduleRun"
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

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<ScheduleAcrRunRequest>(content, GlobalSettings.JsonOptions)
                      ?? new ScheduleAcrRunRequest();

        var operation = _controlPlane.ScheduleRun(
            subscriptionIdentifier, resourceGroupIdentifier, registryName, request);

        if (operation.Result == OperationResult.NotFound)
        {
            response.CreateErrorResponse(operation.Code!, operation.Reason!);
            return;
        }

        var run = operation.Resource!;

        // When the run is asynchronous (Docker execution in progress), return 202 Accepted.
        // Azure-AsyncOperation points to the dedicated status endpoint so the .NET SDK LRO
        // machinery receives {"status":"InProgress"/"Succeeded"/"Failed"} which it understands.
        // Location points to the run GET endpoint so the SDK retrieves the final resource once done.
        if (run.Properties.Status == "Queued" || run.Properties.Status == "Running")
        {
            var scheme = context.Request.Scheme;
            var host = context.Request.Host.Value;
            var basePath =
                $"{scheme}://{host}/subscriptions/{path.ExtractValueFromPath(2)}" +
                $"/resourceGroups/{path.ExtractValueFromPath(4)}" +
                $"/providers/Microsoft.ContainerRegistry/registries/{registryName}";
            var operationStatusUrl =
                $"{basePath}/runs/{run.Properties.RunId}/operationStatuses?api-version=2019-04-01";
            var runGetUrl =
                $"{basePath}/runs/{run.Properties.RunId}?api-version=2019-04-01";
            response.Headers.TryAddWithoutValidation("Azure-AsyncOperation", operationStatusUrl);
            response.Headers.TryAddWithoutValidation("Location", runGetUrl);
            response.StatusCode = HttpStatusCode.Accepted;
        }
        else
        {
            response.StatusCode = HttpStatusCode.OK;
        }

        response.CreateJsonContentResponse(run);
    }
}
