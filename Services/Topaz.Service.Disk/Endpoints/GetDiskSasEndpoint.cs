using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Disk.Endpoints;

internal sealed class GetDiskSasEndpoint : IEndpointDefinition
{
    public string? ProviderNamespace => null;

    public string[] Endpoints => ["GET /disk-sas/{uniqueId}"];

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

        // Parse optional Range header: "bytes=start-end"
        var rangeHeader = context.Request.Headers["Range"].ToString();
        long rangeStart = 0;
        long rangeEnd = store.Size - 1;
        bool isRangeRequest = false;

        if (!string.IsNullOrEmpty(rangeHeader) &&
            rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
        {
            var rangePart = rangeHeader["bytes=".Length..];
            var dashIndex = rangePart.IndexOf('-');
            if (dashIndex > 0 &&
                long.TryParse(rangePart[..dashIndex], out var parsedStart) &&
                long.TryParse(rangePart[(dashIndex + 1)..], out var parsedEnd))
            {
                rangeStart = parsedStart;
                rangeEnd = Math.Min(parsedEnd, store.Size - 1);
                isRangeRequest = true;
            }
        }

        var length = (int)(rangeEnd - rangeStart + 1);
        var buffer = new byte[length];
        store.Read(rangeStart, buffer.AsSpan());

        response.StatusCode = isRangeRequest ? HttpStatusCode.PartialContent : HttpStatusCode.OK;
        response.Content = new ByteArrayContent(buffer);
        response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        response.Content.Headers.ContentLength = length;

        if (isRangeRequest)
            response.Content.Headers.Add("Content-Range",
                $"bytes {rangeStart}-{rangeEnd}/{store.Size}");
    }
}
