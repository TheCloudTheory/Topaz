using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Services;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class PutBlockEndpoint(ITopazLogger logger)
    : BlobDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string[] Endpoints => ["PUT /{containerName}/...?comp=block"];

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
            if (!context.Request.QueryString.TryGetValueForKey("blockid", out var blockId) ||
                string.IsNullOrEmpty(blockId))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var statusCode = _dataPlane.PutBlock(
                subscriptionIdentifier,
                resourceGroupIdentifier,
                storageAccount!.Name,
                context.Request.Path.Value!,
                blockId!,
                context.Request.Body);

            response.StatusCode = statusCode;
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
