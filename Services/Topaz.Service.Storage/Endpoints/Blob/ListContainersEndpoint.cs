using System.Net;
using System.Text;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class ListContainersEndpoint(ITopazLogger logger)
    : BlobDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly BlobServiceControlPlane _controlPlane = new(new BlobResourceProvider(logger));

    public string[] Endpoints => ["GET /"];

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
            Logger.LogDebug(nameof(ListContainersEndpoint), nameof(GetResponse),
                "Handling listing containers for {0}.", storageAccount!.Name);

            var containers = _controlPlane.ListContainers(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name);

            using var sw = new EncodingAwareStringWriter();
            var serializer = new XmlSerializer(typeof(ContainerEnumerationResult));
            serializer.Serialize(sw, containers);

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
