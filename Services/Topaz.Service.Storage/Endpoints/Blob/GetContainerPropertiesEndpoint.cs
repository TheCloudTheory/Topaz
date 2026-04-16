using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class GetContainerPropertiesEndpoint(ITopazLogger logger)
    : BlobDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string[] Endpoints => ["GET /{containerName}?restype=container"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/containers/read"];

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
            var containerName = GetContainerName(context.Request.Path);

            Logger.LogDebug(nameof(GetContainerPropertiesEndpoint), nameof(GetResponse),
                "Getting properties for container: {0}", containerName);

            var op = _dataPlane.GetContainerMetadata(subscriptionIdentifier,
                resourceGroupIdentifier, storageAccount!.Name, containerName);

            response.StatusCode = op.Result == OperationResult.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound;

            if (op.Result != OperationResult.Success) return;

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
