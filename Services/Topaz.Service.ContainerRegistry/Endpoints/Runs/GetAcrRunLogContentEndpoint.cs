using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Runs;

internal sealed class GetAcrRunLogContentEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ContainerRegistryControlPlane _controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => null;

    public string[] Endpoints => ["GET /v2/runs/{runId}/log"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var runId = context.Request.Path.Value!.ExtractValueFromPath(3) ?? string.Empty;
        var logContent = _controlPlane.GetRunLog(runId) ?? "Build succeeded.\n";

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(logContent);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Text.Plain);
    }
}
