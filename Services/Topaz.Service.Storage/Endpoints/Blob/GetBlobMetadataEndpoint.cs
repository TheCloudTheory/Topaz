using System.Net;
using System.Net.Http.Headers;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class GetBlobMetadataEndpoint(ITopazLogger logger)
    : BlobDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string[] Endpoints => ["GET /{containerName}/...?comp=metadata"];

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
            Logger.LogDebug(nameof(GetBlobMetadataEndpoint), nameof(GetResponse),
                "Handling blob metadata for {0}.", context.Request.Path.Value);

            var op = _dataPlane.GetBlobMetadata(subscriptionIdentifier,
                resourceGroupIdentifier, storageAccount!.Name, context.Request.Path.Value!);

            if (op.Result == OperationResult.NotFound)
            {
                response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Blob not found",
                    HttpStatusCode.NotFound);
                return;
            }

            response.StatusCode = HttpStatusCode.OK;

            var now = DateTimeOffset.UtcNow;
            response.Headers.ETag = new EntityTagHeaderValue($"\"{now.Ticks}\"");

            if (op.Resource != null)
            {
                foreach (var (key, value) in op.Resource)
                    response.Headers.TryAddWithoutValidation(key, value);
            }

            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
            response.Content.Headers.TryAddWithoutValidation("Last-Modified", now.ToString("R"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
