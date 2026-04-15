using System.Net;
using System.Text;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class ListBlobsEndpoint(ITopazLogger logger)
    : BlobDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string[] Endpoints => ["GET /{containerName}?restype=container&comp=list"];

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
            var containerName = GetContainerName(context.Request.Path);

            Logger.LogDebug(nameof(ListBlobsEndpoint), nameof(GetResponse),
                "Handling listing blobs for {0}/{1}.", storageAccount!.Name, containerName);

            // TODO: The request may come with additional keys in the query string, e.g.:
            // ?restype=container&comp=list&prefix=localhost/eh-test/$default/ownership/&include=Metadata
            // We need to handle them as well

            var blobs = _dataPlane.ListBlobs(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name,
                containerName);

            using var sw = new EncodingAwareStringWriter();
            var serializer = new XmlSerializer(typeof(BlobEnumerationResult));
            serializer.Serialize(sw, blobs);

            response.Content = new StringContent(sw.ToString(), Encoding.UTF8, "application/xml");
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
