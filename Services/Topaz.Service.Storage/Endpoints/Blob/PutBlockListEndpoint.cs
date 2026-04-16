using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Services;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class PutBlockListEndpoint(ITopazLogger logger)
    : BlobDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string[] Endpoints => ["PUT /{containerName}/...?comp=blocklist"];

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
            if (!TryGetBlobName(context.Request.Path.Value!, out var blobName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var contentType = context.Request.Headers.TryGetValue("x-ms-blob-content-type", out var blobContentType)
                ? blobContentType.ToString()
                : null;

            var op = _dataPlane.PutBlockList(
                subscriptionIdentifier,
                resourceGroupIdentifier,
                storageAccount!.Name,
                context.Request.Path.Value!,
                blobName!,
                context.Request.Body,
                contentType);

            response.StatusCode = op.Result == OperationResult.Created ? HttpStatusCode.Created : HttpStatusCode.BadRequest;

            if (op.Resource == null)
            {
                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                return;
            }

            SetResponseHeaders(response, op.Resource);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
