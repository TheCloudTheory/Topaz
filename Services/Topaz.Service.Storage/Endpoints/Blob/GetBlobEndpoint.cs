using System.Net;
using System.Net.Http.Headers;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class GetBlobEndpoint(ITopazLogger logger)
    : BlobDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string[] Endpoints => ["GET /{containerName}/..."];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/containers/blobs/read"];

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
            if (!TryGetBlobName(context.Request.Path.Value!, out var blobName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            Logger.LogDebug(nameof(GetBlobEndpoint), nameof(GetResponse),
                "Handling blob download for {0}.", context.Request.Path.Value);

            var (code, content) = _dataPlane.GetBlob(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name, context.Request.Path.Value!);

            if (code == HttpStatusCode.NotFound)
            {
                response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Blob not found",
                    HttpStatusCode.NotFound);
                return;
            }

            var (_, properties) = _dataPlane.GetBlobProperties(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name, context.Request.Path.Value!, blobName!);

            response.StatusCode = HttpStatusCode.OK;

            var bytes = content != null ? System.Text.Encoding.UTF8.GetBytes(content) : [];
            response.Content = new ByteArrayContent(bytes);

            var contentType = properties?.ContentType ?? "application/octet-stream";
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            response.Content.Headers.ContentLength = bytes.Length;

            if (properties != null)
            {
                var etag = properties.ETag.ToString();
                response.Headers.ETag = new EntityTagHeaderValue(
                    etag.StartsWith('"') ? etag : $"\"{etag}\"");

                response.Headers.TryAddWithoutValidation("x-ms-blob-type", properties.BlobType);
                response.Headers.TryAddWithoutValidation("x-ms-server-encrypted", "true");
                response.Headers.TryAddWithoutValidation("x-ms-lease-status", "unlocked");
                response.Headers.TryAddWithoutValidation("x-ms-lease-state", "available");

                if (!string.IsNullOrEmpty(properties.LastModified))
                    response.Content.Headers.TryAddWithoutValidation("Last-Modified", properties.LastModified);

                if (!string.IsNullOrEmpty(properties.ContentEncoding))
                    response.Content.Headers.TryAddWithoutValidation("Content-Encoding",
                        properties.ContentEncoding);

                if (!string.IsNullOrEmpty(properties.ContentLanguage))
                    response.Content.Headers.TryAddWithoutValidation("Content-Language",
                        properties.ContentLanguage);

                if (!string.IsNullOrEmpty(properties.CacheControl))
                    response.Content.Headers.TryAddWithoutValidation("Cache-Control", properties.CacheControl);

                if (!string.IsNullOrEmpty(properties.ContentDisposition))
                    response.Content.Headers.TryAddWithoutValidation("Content-Disposition",
                        properties.ContentDisposition);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
