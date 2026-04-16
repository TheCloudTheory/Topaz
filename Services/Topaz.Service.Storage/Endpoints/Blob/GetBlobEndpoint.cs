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

            var op = _dataPlane.GetBlob(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name, context.Request.Path.Value!);

            if (op.Result == OperationResult.NotFound)
            {
                response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Blob not found",
                    HttpStatusCode.NotFound);
                return;
            }

            var props = _dataPlane.GetBlobProperties(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name, context.Request.Path.Value!, blobName!);

            response.StatusCode = HttpStatusCode.OK;

            var bytes = op.Resource != null ? System.Text.Encoding.UTF8.GetBytes(op.Resource) : [];
            response.Content = new ByteArrayContent(bytes);

            var contentType = props.Resource?.ContentType ?? "application/octet-stream";
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            response.Content.Headers.ContentLength = bytes.Length;

            if (props.Resource != null)
            {
                var etag = props.Resource.ETag.ToString();
                response.Headers.ETag = new EntityTagHeaderValue(
                    etag.StartsWith('"') ? etag : $"\"{etag}\"");

                response.Headers.TryAddWithoutValidation("x-ms-blob-type", props.Resource.BlobType);
                response.Headers.TryAddWithoutValidation("x-ms-server-encrypted", "true");
                response.Headers.TryAddWithoutValidation("x-ms-lease-status", "unlocked");
                response.Headers.TryAddWithoutValidation("x-ms-lease-state", "available");

                if (!string.IsNullOrEmpty(props.Resource.LastModified))
                    response.Content.Headers.TryAddWithoutValidation("Last-Modified", props.Resource.LastModified);

                if (!string.IsNullOrEmpty(props.Resource.ContentEncoding))
                    response.Content.Headers.TryAddWithoutValidation("Content-Encoding",
                        props.Resource.ContentEncoding);

                if (!string.IsNullOrEmpty(props.Resource.ContentLanguage))
                    response.Content.Headers.TryAddWithoutValidation("Content-Language",
                        props.Resource.ContentLanguage);

                if (!string.IsNullOrEmpty(props.Resource.CacheControl))
                    response.Content.Headers.TryAddWithoutValidation("Cache-Control", props.Resource.CacheControl);

                if (!string.IsNullOrEmpty(props.Resource.ContentDisposition))
                    response.Content.Headers.TryAddWithoutValidation("Content-Disposition",
                        props.Resource.ContentDisposition);
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
