using Topaz.EventPipeline;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class SnapshotBlobEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : BlobDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string? ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints => ["PUT /{containerName}/...?comp=snapshot"];

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

        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name, Permissions, context, response))
            return;

        try
        {
            var blobPath = context.Request.Path.Value!;
            var leaseId = context.Request.Headers.TryGetValue("x-ms-lease-id", out var leaseIdValues)
                ? leaseIdValues.ToString()
                : null;

            Logger.LogDebug(nameof(SnapshotBlobEndpoint), nameof(GetResponse),
                "Creating snapshot for blob: {0}", blobPath);

            var snapshotMetadata = context.Request.Headers
                .Where(h => h.Key.StartsWith("x-ms-meta-", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(h => h.Key["x-ms-meta-".Length..], h => h.Value.ToString());

            var op = _dataPlane.SnapshotBlob(
                subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name, blobPath,
                leaseId, snapshotMetadata.Count > 0 ? snapshotMetadata : null);

            if (op.Result == OperationResult.NotFound)
            {
                response.CreateBlobErrorResponse("BlobNotFound", $"The specified blob '{blobPath}' does not exist.", HttpStatusCode.NotFound);
                return;
            }

            if (op.Result == OperationResult.PreconditionFailed)
            {
                response.CreateBlobErrorResponse("LeaseIdMismatchWithBlobOperation", "The lease ID specified did not match the lease ID for the blob.", HttpStatusCode.PreconditionFailed);
                return;
            }

            response.StatusCode = HttpStatusCode.Created;
            response.Headers.TryAddWithoutValidation("x-ms-snapshot", op.Resource!);
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
            response.Content.Headers.TryAddWithoutValidation("Last-Modified", DateTimeOffset.UtcNow.ToString("R"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
