using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class CreateContainerEndpoint(ITopazLogger logger)
    : BlobDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly BlobServiceControlPlane _controlPlane = new(new BlobResourceProvider(logger));
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string[] Endpoints => ["PUT /{containerName}"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/containers/write"];

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

            if (context.Request.QueryString.TryGetValueForKey("comp", out var comp) && comp == "metadata")
            {
                HandleSetContainerMetadataRequest(subscriptionIdentifier, resourceGroupIdentifier,
                    storageAccount!.Name, containerName, context.Request.Headers, response);
            }
            else
            {
                HandleCreateContainerRequest(subscriptionIdentifier, resourceGroupIdentifier,
                    storageAccount!.Name, containerName, response);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }

    private void HandleCreateContainerRequest(
        Service.Shared.Domain.SubscriptionIdentifier subscriptionIdentifier,
        Service.Shared.Domain.ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string containerName,
        HttpResponseMessage response)
    {
        Logger.LogDebug(nameof(CreateContainerEndpoint), nameof(HandleCreateContainerRequest),
            "Creating container: {0}", containerName);

        var code = _controlPlane.CreateContainer(subscriptionIdentifier, resourceGroupIdentifier, containerName,
            storageAccountName);

        response.StatusCode = code;
    }

    private void HandleSetContainerMetadataRequest(
        Service.Shared.Domain.SubscriptionIdentifier subscriptionIdentifier,
        Service.Shared.Domain.ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string containerName,
        IHeaderDictionary headers,
        HttpResponseMessage response)
    {
        Logger.LogDebug(nameof(CreateContainerEndpoint), nameof(HandleSetContainerMetadataRequest),
            "Setting metadata for container: {0}", containerName);

        var result = _dataPlane.SetContainerMetadata(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, containerName, headers);

        response.StatusCode = result;

        if (result != HttpStatusCode.OK) return;

        var now = DateTimeOffset.UtcNow;
        response.Headers.ETag = new EntityTagHeaderValue($"\"{now.Ticks}\"");
        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
        response.Content.Headers.TryAddWithoutValidation("Last-Modified", now.ToString("R"));
    }
}
