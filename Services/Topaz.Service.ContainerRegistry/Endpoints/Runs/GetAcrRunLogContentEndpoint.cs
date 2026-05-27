using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Runs;

internal sealed class GetAcrRunLogContentEndpoint : IEndpointDefinition
{
    public string? ProviderNamespace => null;

    public string[] Endpoints => ["GET /v2/runs/{runId}/log"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        const string logBody = "Build succeeded.\n";
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(logBody);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Text.Plain);
    }
}
