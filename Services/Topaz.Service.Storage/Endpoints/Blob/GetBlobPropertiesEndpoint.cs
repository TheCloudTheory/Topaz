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

            var op = _dataPlane.GetBlobProperties(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name, context.Request.Path.Value!, blobName!);

            if (op.Result == OperationResult.NotFound)
            {
                response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Blob not found",
                    HttpStatusCode.NotFound);
                return;
            }

            response.StatusCode = HttpStatusCode.OK;

            if (op.Resource != null)
            {
                var etag = op.Resource.ETag.ToString();
                response.Headers.ETag = new EntityTagHeaderValue(
                    etag.StartsWith('"') ? etag : $"\"{etag}\"");

                response.Headers.TryAddWithoutValidation("x-ms-blob-type", op.Resource.BlobType);
                response.Headers.TryAddWithoutValidation("x-ms-server-encrypted", "true");
                response.Headers.TryAddWithoutValidation("x-ms-lease-status", "unlocked");
                response.Headers.TryAddWithoutValidation("x-ms-lease-state", "available");

                if (!string.IsNullOrEmpty(op.Resource.DateUploaded))
                    response.Headers.TryAddWithoutValidation("x-ms-creation-time", op.Resource.DateUploaded);

                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType =
                    MediaTypeHeaderValue.Parse(op.Resource.ContentType);
                response.Content.Headers.ContentLength = op.Resource.ContentLength;

                if (!string.IsNullOrEmpty(op.Resource.LastModified))
                    response.Content.Headers.TryAddWithoutValidation("Last-Modified", op.Resource.LastModified);

                if (!string.IsNullOrEmpty(op.Resource.ContentEncoding))
                    response.Content.Headers.TryAddWithoutValidation("Content-Encoding", op.Resource.ContentEncoding);

                if (!string.IsNullOrEmpty(op.Resource.ContentLanguage))
                    response.Content.Headers.TryAddWithoutValidation("Content-Language", op.Resource.ContentLanguage);

                if (!string.IsNullOrEmpty(op.Resource.CacheControl))
                    response.Headers.TryAddWithoutValidation("Cache-Control", op.Resource.CacheControl);

                if (!string.IsNullOrEmpty(op.Resource.ContentDisposition))
                    response.Content.Headers.TryAddWithoutValidation("Content-Disposition", op.Resource.ContentDisposition);

                if (!string.IsNullOrEmpty(op.Resource.CopyId))
                {
                    response.Headers.TryAddWithoutValidation("x-ms-copy-id", op.Resource.CopyId);
                    response.Headers.TryAddWithoutValidation("x-ms-copy-status", "success");
                }
            }

            var metaOp = _dataPlane.GetBlobMetadata(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name, context.Request.Path.Value!);

            if (metaOp.Resource != null)
            {
                foreach (var (key, value) in metaOp.Resource)
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
