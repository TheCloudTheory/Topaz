using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Disk.Endpoints;

internal sealed class HeadDiskSasEndpoint : IEndpointDefinition
{
    public string? ProviderNamespace => null;

    public string[] Endpoints => ["HEAD /disk-sas/{uniqueId}"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var uniqueIdStr = context.Request.Path.Value.ExtractValueFromPath(2);

        if (!Guid.TryParse(uniqueIdStr, out var uniqueId) ||
            DiskByteStore.Instance.TryGet(uniqueId) is not { } store)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        response.Content.Headers.ContentLength = store.Size;
    }
}
