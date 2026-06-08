using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Disk.Endpoints;

internal sealed class DiskSasEndpoint : IEndpointDefinition
{
    public string? ProviderNamespace => null;

    public string[] Endpoints => ["GET /disk-sas/{uniqueId}"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        response.Content.Headers.ContentLength = 0;
    }
}
