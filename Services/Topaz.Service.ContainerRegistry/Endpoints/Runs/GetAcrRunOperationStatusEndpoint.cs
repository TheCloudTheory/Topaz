using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ContainerRegistry.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Runs;

/// <summary>
/// Returns the ARM async-operation status for an ACR run.
/// The Azure-AsyncOperation header on 202 responses from scheduleRun / tasks/{name}/run
/// points here so the .NET SDK LRO machinery can poll for terminal state.
/// </summary>
internal sealed class GetAcrRunOperationStatusEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ContainerRegistryControlPlane _controlPlane =
        ContainerRegistryControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ContainerRegistry";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}/runs/{runId}/operationStatuses"
    ];

    public string[] Permissions => ["Microsoft.ContainerRegistry/registries/runs/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
        var registryName = path.ExtractValueFromPath(8)!;
        var runId = path.ExtractValueFromPath(10)!;

        var operation = _controlPlane.GetRun(
            subscriptionIdentifier, resourceGroupIdentifier, registryName, runId);

        if (operation.Result == OperationResult.NotFound)
        {
            response.CreateErrorResponse(operation.Code!, operation.Reason!);
            return;
        }

        var runStatus = operation.Resource!.Properties.Status;
        var statusResponse = new AcrRunOperationStatusResponse
        {
            Status = AcrRunOperationStatusResponse.FromRunStatus(runStatus)
        };

        response.StatusCode = HttpStatusCode.OK;
        response.CreateJsonContentResponse(statusResponse);
        response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json);
    }
}
