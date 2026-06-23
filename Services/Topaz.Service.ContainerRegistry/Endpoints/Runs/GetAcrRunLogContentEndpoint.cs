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
        var logBytes = System.Text.Encoding.UTF8.GetBytes(logContent);

        var rangeHeader = context.Request.Headers["Range"].ToString();
        long start = 0;
        long end = logBytes.Length - 1;
        if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes="))
        {
            var parts = rangeHeader["bytes=".Length..].Split('-');
            start = long.Parse(parts[0]);
            end = string.IsNullOrEmpty(parts[1]) ? logBytes.Length - 1 : Math.Min(long.Parse(parts[1]), logBytes.Length - 1);
        }

        var slice = logBytes[(int)start..(int)(end + 1)];
        response.StatusCode = HttpStatusCode.PartialContent;
        response.Content = new ByteArrayContent(slice);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Text.Plain);
        response.Content.Headers.ContentRange = new ContentRangeHeaderValue(start, end, logBytes.Length);
    }
}
