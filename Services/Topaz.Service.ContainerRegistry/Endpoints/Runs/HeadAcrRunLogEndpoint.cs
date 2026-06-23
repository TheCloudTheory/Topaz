using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Endpoints.Runs;

internal sealed class HeadAcrRunLogEndpoint : IEndpointDefinition
{
    public string? ProviderNamespace => null;

    public string[] Endpoints => ["HEAD /v2/runs/{runId}/log"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentLength = 0;
        response.Headers.Add("x-ms-meta-Complete", "Succeeded");
    }
}
