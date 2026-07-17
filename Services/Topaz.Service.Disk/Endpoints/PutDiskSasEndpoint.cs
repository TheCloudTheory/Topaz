using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Disk.Endpoints;

internal sealed class PutDiskSasEndpoint : IEndpointDefinition
{
    public string? ProviderNamespace => null;

    public string[] Endpoints => ["PUT /disk-sas/{uniqueId}"];

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

        // Parse Content-Range: bytes start-end/total
        long offset = 0;
        var contentRange = context.Request.Headers["Content-Range"].ToString();
        if (!string.IsNullOrEmpty(contentRange) &&
            contentRange.StartsWith("bytes ", StringComparison.OrdinalIgnoreCase))
        {
            var rangePart = contentRange["bytes ".Length..];
            var slashIndex = rangePart.IndexOf('/');
            var rangePortion = slashIndex > 0 ? rangePart[..slashIndex] : rangePart;
            var dashIndex = rangePortion.IndexOf('-');
            if (dashIndex > 0 && long.TryParse(rangePortion[..dashIndex], out var parsedStart))
                offset = parsedStart;
        }

        using var ms = new MemoryStream();
        context.Request.Body.CopyTo(ms);
        var data = ms.ToArray();

        store.Write(offset, data.AsSpan());

        response.StatusCode = HttpStatusCode.Created;
        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
    }
}
