using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ContainerRegistry.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Runs;

internal sealed class GetAcrRunLogSasUrlEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ContainerRegistryControlPlane _controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ContainerRegistry";

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}/runs/{runId}/listLogSasUrl"
    ];

    public string[] Permissions => ["Microsoft.ContainerRegistry/registries/runs/listLogSasUrl/action"];

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

        var logLink = $"https://{registryName}.cr.topaz.local.dev:{GlobalSettings.ContainerRegistryPort}/v2/runs/{runId}/log?token={runId}";
        var result = new GetAcrRunLogSasUrlResponse { LogLink = logLink };

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(result.ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
