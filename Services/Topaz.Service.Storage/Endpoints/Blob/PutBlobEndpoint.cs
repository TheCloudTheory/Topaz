using System.Net;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class PutBlobEndpoint(ITopazLogger logger)
    : BlobDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string[] Endpoints => ["PUT /{containerName}/..."];

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

            if (context.Request.QueryString.TryGetValueForKey("comp", out var comp) && comp == "metadata")
            {
                HandleSetBlobMetadataRequest(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name,
                    context.Request.Path, blobName!, context.Request.Headers, response);
            }
            else
            {
                HandleUploadBlobRequest(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name,
                    context.Request.Path, blobName!, context.Request.Body, response);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }

    private void HandleUploadBlobRequest(
        Service.Shared.Domain.SubscriptionIdentifier subscriptionIdentifier,
        Service.Shared.Domain.ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string blobPath,
        string blobName,
        Stream input,
        HttpResponseMessage response)
    {
        Logger.LogDebug(nameof(PutBlobEndpoint), nameof(HandleUploadBlobRequest), "Handling blob upload for {0}.",
            blobPath);

        var result = _dataPlane.PutBlob(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, blobPath, blobName, input);

        // TODO: The response must include the response headers from https://learn.microsoft.com/en-us/rest/api/storageservices/put-blob?tabs=microsoft-entra-id#response
        response.StatusCode = result.code;

        if (result.properties == null) return;

        SetResponseHeaders(response, result.properties);
    }

    private void HandleSetBlobMetadataRequest(
        Service.Shared.Domain.SubscriptionIdentifier subscriptionIdentifier,
        Service.Shared.Domain.ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string blobPath,
        string blobName,
        IHeaderDictionary headers,
        HttpResponseMessage response)
    {
        Logger.LogDebug(nameof(PutBlobEndpoint), nameof(HandleSetBlobMetadataRequest),
            "Handling setting blob metadata for {0}.", blobPath);

        var result = _dataPlane.SetBlobMetadata(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
            blobPath, headers);

        if (result == HttpStatusCode.NotFound)
        {
            response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Blob not found", HttpStatusCode.NotFound);
            Logger.LogDebug(nameof(PutBlobEndpoint), nameof(HandleSetBlobMetadataRequest),
                "Blob {0} was not found.", blobPath);
        }
        else
        {
            var properties = _dataPlane.GetBlobProperties(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccountName, blobPath, blobName);

            response.StatusCode = result;

            if (properties.properties == null) return;

            SetResponseHeaders(response, properties.properties);
        }
    }
}
