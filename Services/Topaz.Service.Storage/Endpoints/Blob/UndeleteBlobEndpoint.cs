using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class UndeleteBlobEndpoint(ITopazLogger logger)
    : BlobDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string[] Endpoints => ["PUT /{containerName}/...?comp=undelete"];

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
            var blobPath = context.Request.Path.Value!;

            Logger.LogDebug(nameof(UndeleteBlobEndpoint), nameof(GetResponse),
                "Undeleting blob: {0}", blobPath);

            var op = _dataPlane.UndeleteBlob(
                subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name, blobPath);

            if (op.Result == OperationResult.NotFound)
            {
                response.CreateBlobErrorResponse("BlobNotFound", $"The specified blob '{blobPath}' does not exist.", HttpStatusCode.NotFound);
                return;
            }

            response.StatusCode = HttpStatusCode.OK;
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
