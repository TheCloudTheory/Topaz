using System.Net;
using System.Net.Http.Headers;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class GetBlobPropertiesEndpoint(ITopazLogger logger)
    : BlobDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string[] Endpoints => ["HEAD /{containerName}/..."];

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

            Logger.LogDebug(nameof(GetBlobPropertiesEndpoint), nameof(GetResponse),
                "Handling blob properties for {0}.", context.Request.Path.Value);

            var (code, properties) = _dataPlane.GetBlobProperties(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name, context.Request.Path.Value!, blobName!);

            if (code == HttpStatusCode.NotFound)
            {
                response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Blob not found",
                    HttpStatusCode.NotFound);
                return;
            }

            response.StatusCode = code;

            if (properties != null)
            {
                var etag = properties.ETag.ToString();
                response.Headers.ETag = new EntityTagHeaderValue(
                    etag.StartsWith('"') ? etag : $"\"{etag}\"");

                response.Headers.TryAddWithoutValidation("x-ms-blob-type", properties.BlobType);
                response.Headers.TryAddWithoutValidation("x-ms-server-encrypted", "true");
                response.Headers.TryAddWithoutValidation("x-ms-lease-status", "unlocked");
                response.Headers.TryAddWithoutValidation("x-ms-lease-state", "available");

                if (!string.IsNullOrEmpty(properties.DateUploaded))
                    response.Headers.TryAddWithoutValidation("x-ms-creation-time", properties.DateUploaded);

                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType =
                    MediaTypeHeaderValue.Parse(properties.ContentType);
                response.Content.Headers.ContentLength = properties.ContentLength;

                if (!string.IsNullOrEmpty(properties.LastModified))
                    response.Content.Headers.TryAddWithoutValidation("Last-Modified", properties.LastModified);

                if (!string.IsNullOrEmpty(properties.ContentEncoding))
                    response.Content.Headers.TryAddWithoutValidation("Content-Encoding", properties.ContentEncoding);

                if (!string.IsNullOrEmpty(properties.ContentLanguage))
                    response.Content.Headers.TryAddWithoutValidation("Content-Language", properties.ContentLanguage);

                if (!string.IsNullOrEmpty(properties.CacheControl))
                    response.Content.Headers.TryAddWithoutValidation("Cache-Control", properties.CacheControl);

                if (!string.IsNullOrEmpty(properties.ContentDisposition))
                    response.Content.Headers.TryAddWithoutValidation("Content-Disposition", properties.ContentDisposition);
            }

            var (_, metadata) = _dataPlane.GetBlobMetadata(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name, context.Request.Path.Value!);

            if (metadata != null)
            {
                foreach (var (key, value) in metadata)
                    response.Headers.TryAddWithoutValidation(key, value);
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
