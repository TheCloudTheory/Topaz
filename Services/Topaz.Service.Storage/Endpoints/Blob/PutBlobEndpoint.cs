using System.Net;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Services;
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
            else if (context.Request.Headers.TryGetValue("x-ms-copy-source", out var copySource) &&
                     !string.IsNullOrEmpty(copySource))
            {
                HandleCopyBlobRequest(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name,
                    context.Request.Path.Value!, blobName!, copySource!, response);
            }
            else
            {
                // Prefer x-ms-blob-content-type (set by SDK via BlobHttpHeaders.ContentType)
                // over the HTTP Content-Type header (which reflects the request body encoding).
                var contentType = context.Request.Headers.TryGetValue("x-ms-blob-content-type", out var blobContentType)
                    ? blobContentType.ToString()
                    : context.Request.ContentType;

                HandleUploadBlobRequest(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name,
                    context.Request.Path, blobName!, context.Request.Body, context.Request.Headers, response, contentType);
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
        IHeaderDictionary requestHeaders,
        HttpResponseMessage response,
        string? contentType = null)
    {
        Logger.LogDebug(nameof(PutBlobEndpoint), nameof(HandleUploadBlobRequest), "Handling blob upload for {0}.",
            blobPath);

        requestHeaders.TryGetValue("x-ms-blob-type", out var blobTypeValue);
        var blobType = blobTypeValue.FirstOrDefault();

        long? pageBlobSize = null;
        if (blobType == "PageBlob" &&
            requestHeaders.TryGetValue("x-ms-blob-content-length", out var pageSizeValue) &&
            long.TryParse(pageSizeValue.FirstOrDefault(), out var parsedSize))
        {
            pageBlobSize = parsedSize;
        }

        var op = _dataPlane.PutBlob(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, blobPath, blobName, input, contentType, blobType, pageBlobSize);

        // TODO: The response must include the response headers from https://learn.microsoft.com/en-us/rest/api/storageservices/put-blob?tabs=microsoft-entra-id#response
        response.StatusCode = op.Result == OperationResult.Created ? HttpStatusCode.Created : HttpStatusCode.BadRequest;

        if (op.Resource != null) SetResponseHeaders(response, op.Resource);
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

        if (result.Result == OperationResult.NotFound)
        {
            response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Blob not found", HttpStatusCode.NotFound);
            Logger.LogDebug(nameof(PutBlobEndpoint), nameof(HandleSetBlobMetadataRequest),
                "Blob {0} was not found.", blobPath);
        }
        else
        {
            var props = _dataPlane.GetBlobProperties(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccountName, blobPath, blobName);

            response.StatusCode = HttpStatusCode.OK;

            if (props.Resource != null) SetResponseHeaders(response, props.Resource);
        }
    }

    private void HandleCopyBlobRequest(
        SubscriptionIdentifier dstSubscriptionId,
        ResourceGroupIdentifier dstResourceGroupId,
        string dstAccountName,
        string dstBlobPath,
        string dstBlobName,
        string copySourceUrl,
        HttpResponseMessage response)
    {
        Logger.LogDebug(nameof(PutBlobEndpoint), nameof(HandleCopyBlobRequest),
            "Handling copy blob to {0} from {1}.", dstBlobPath, copySourceUrl);

        if (!Uri.TryCreate(copySourceUrl, UriKind.Absolute, out var sourceUri))
        {
            Logger.LogError($"Invalid x-ms-copy-source URL: {copySourceUrl}");
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var srcAccountName = sourceUri.Host.Split('.')[0];
        var srcBlobPath = sourceUri.AbsolutePath;

        var srcIdentifiers = GlobalDnsEntries.GetEntry(AzureStorageService.UniqueName, srcAccountName);
        if (srcIdentifiers == null)
        {
            response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Source blob not found", HttpStatusCode.NotFound);
            return;
        }

        var srcSubscriptionId = SubscriptionIdentifier.From(srcIdentifiers.Value.subscription);
        var srcResourceGroupId = ResourceGroupIdentifier.From(srcIdentifiers.Value.resourceGroup);

        var op = _dataPlane.CopyBlob(
            srcSubscriptionId, srcResourceGroupId, srcAccountName, srcBlobPath,
            dstSubscriptionId, dstResourceGroupId, dstAccountName, dstBlobPath, dstBlobName);

        if (op.Result == OperationResult.NotFound)
        {
            response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Source blob not found", HttpStatusCode.NotFound);
            return;
        }

        response.StatusCode = HttpStatusCode.Accepted;
        response.Headers.TryAddWithoutValidation("x-ms-copy-id", op.Resource!.CopyId);
        response.Headers.TryAddWithoutValidation("x-ms-copy-status", "success");

        if (op.Resource.Properties != null)
            SetResponseHeaders(response, op.Resource.Properties);
    }
}
