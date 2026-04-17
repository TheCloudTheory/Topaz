using System.Net;
using System.Net.Http.Headers;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class PutPageEndpoint(ITopazLogger logger)
    : BlobDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string[] Endpoints => ["PUT /{containerName}/...?comp=page"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/containers/blobs/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultBlobStoragePort], Protocol.Http);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();

        try
        {
            if (!context.Request.Headers.TryGetValue("x-ms-range", out var rangeValue) ||
                string.IsNullOrEmpty(rangeValue))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.CreateBlobErrorResponse(BlobErrorCode.InvalidRange, "x-ms-range header is required.", HttpStatusCode.BadRequest);
                return;
            }

            if (!TryParseRange(rangeValue!, out var startByte, out var endByte))
            {
                response.CreateBlobErrorResponse(BlobErrorCode.InvalidRange, "Invalid x-ms-range header.", HttpStatusCode.BadRequest);
                return;
            }

            var pageWrite = context.Request.Headers.TryGetValue("x-ms-page-write", out var pageWriteValue)
                ? pageWriteValue.ToString()
                : "update";

            var op = _dataPlane.PutPage(
                subscriptionIdentifier,
                resourceGroupIdentifier,
                storageAccount!.Name,
                context.Request.Path.Value!,
                startByte,
                endByte,
                pageWrite,
                context.Request.Body);

            if (op.Result == OperationResult.NotFound)
            {
                response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Blob not found.", HttpStatusCode.NotFound);
                return;
            }

            if (op.Result == OperationResult.BadRequest)
            {
                response.CreateBlobErrorResponse(BlobErrorCode.InvalidRange, op.Reason ?? "Bad request.", HttpStatusCode.BadRequest);
                return;
            }

            response.StatusCode = HttpStatusCode.Created;
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

            if (op.Resource != null)
            {
                var etag = op.Resource.ETag.ToString();
                if (!etag.StartsWith('"')) etag = $"\"{etag}\"";
                response.Headers.TryAddWithoutValidation("ETag", etag);

                if (!string.IsNullOrEmpty(op.Resource.LastModified))
                    response.Content.Headers.TryAddWithoutValidation("Last-Modified", op.Resource.LastModified);

                response.Headers.TryAddWithoutValidation("x-ms-blob-sequence-number", "0");
                response.Headers.TryAddWithoutValidation("x-ms-request-server-encrypted", "true");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }

    private static bool TryParseRange(string rangeHeader, out long startByte, out long endByte)
    {
        startByte = 0;
        endByte = 0;

        // Format: bytes=<startByte>-<endByte>
        var withoutPrefix = rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase)
            ? rangeHeader["bytes=".Length..]
            : rangeHeader;

        var parts = withoutPrefix.Split('-');
        if (parts.Length != 2) return false;

        return long.TryParse(parts[0], out startByte) && long.TryParse(parts[1], out endByte);
    }
}
