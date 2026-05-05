using Topaz.EventPipeline;
using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class CreateContainerEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : BlobDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly BlobServiceControlPlane _controlPlane = new(new BlobResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints => ["PUT /{containerName}"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/containers/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultBlobStoragePort], Protocol.Https);

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
            var containerName = GetContainerName(context.Request.Path);

            Logger.LogDebug(nameof(CreateContainerEndpoint), nameof(GetResponse),
                "Creating container: {0}", containerName);

            var op = _controlPlane.CreateContainer(subscriptionIdentifier, resourceGroupIdentifier, containerName,
                storageAccount!.Name);

            response.StatusCode = op.Result == OperationResult.Created
                ? HttpStatusCode.Created
                : HttpStatusCode.BadRequest;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
